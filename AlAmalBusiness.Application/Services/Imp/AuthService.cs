using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepo _authRepo;
        private readonly IRefreshTokenRepo _refreshTokenRepo;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthService(
            IAuthRepo authRepo,
            IRefreshTokenRepo refreshTokenRepo,
            ITokenService tokenService,
            IConfiguration config)
        {
            _authRepo = authRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _tokenService = tokenService;
            _config = config;
        }

        private int AccessMinutes => _config.GetValue<int?>("JwtSettings:ExpiryMinutes") ?? 15;
        private int RefreshDays => _config.GetValue<int?>("JwtSettings:RefreshDays") ?? 7;
        // How long a spent (rotated or signed-out) row is kept so that
        // replaying it still trips the leak check. Two days covers the
        // realistic window — a client holds the token it was last given, so
        // anything older turning up is already past being actionable.
        private int RevokedRetentionHours => _config.GetValue<int?>("JwtSettings:RevokedRetentionHours") ?? 48;
        // The sweep is cheap but pointless to repeat; once an hour per
        // process is plenty when rotation adds tens of rows a day.
        private int SweepEveryMinutes => _config.GetValue<int?>("JwtSettings:SweepEveryMinutes") ?? 60;

        // Last sweep, as UTC ticks, shared across this process. Zero means
        // "not since startup", so the first sign-in after a recycle sweeps.
        private static long _lastSweepTicks;

        public async Task<LoginResult> LoginAsync(LoginDTO loginDto)
        {
            var (user, error) = await _authRepo.LogInAsync(loginDto.UserName!, loginDto.Password!);
            if (user == null)
                return LoginResult.Fail(error ?? "User or Password is Incorrect");

            var roles = (await _authRepo.GetRolesAsync(loginDto.UserName!)).ToList();
            if (roles.Count == 0)
                return LoginResult.Fail("User has no roles assigned.");

            return await IssueAsync(user, roles);
        }

        // Rotation: the presented token is spent and replaced. Reusing one
        // that has already been spent means the value leaked — the whole
        // chain for that user is cut off rather than quietly issuing another.
        public async Task<LoginResult> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return LoginResult.Fail("Your session has expired. Please log in again.");

            var stored = await _refreshTokenRepo.GetByHashAsync(Hash(refreshToken));
            if (stored == null)
                return LoginResult.Fail("Your session has expired. Please log in again.");

            var now = DateTime.UtcNow;
            if (!stored.IsActive(now))
            {
                // Already rotated away or revoked, yet presented again.
                if (stored.RevokedAt != null)
                    await _refreshTokenRepo.RevokeAllForUserAsync(stored.UserId);
                return LoginResult.Fail("Your session has expired. Please log in again.");
            }

            // Re-read the user each time, so a disabled account or a role
            // change takes effect on the next refresh rather than lasting as
            // long as someone keeps a browser open.
            var user = await _authRepo.FindActiveByIdAsync(stored.UserId);
            if (user == null)
            {
                stored.RevokedAt = now;
                await _refreshTokenRepo.SaveChangesAsync();
                return LoginResult.Fail("Your account is inactive. Please contact support.");
            }

            var roles = (await _authRepo.GetRolesByIdAsync(stored.UserId)).ToList();
            if (roles.Count == 0)
            {
                stored.RevokedAt = now;
                await _refreshTokenRepo.SaveChangesAsync();
                return LoginResult.Fail("User has no roles assigned.");
            }

            var issued = await IssueAsync(user, roles, spend: stored);
            return issued;
        }

        // Rotation is what fills this table — every refresh spends one row and
        // writes another — so the sweep has to keep up with it, not with the
        // far rarer sign-in. It used to run on a 1-in-50 roll and clear only
        // rows a week PAST expiry, which on a system with a handful of daily
        // logins meant it effectively never ran: 595 rows had accumulated for
        // 15 usable sessions. Now it runs on a clock, and clears a row as soon
        // as it is expired (RefreshAsync refuses those anyway) or has been
        // spent longer than RevokedRetentionHours.
        //
        // Time-gated per process rather than per call, and the slot is claimed
        // before the work so two simultaneous sign-ins don't both sweep.
        // Never throws: cleanup must not be able to fail a sign-in.
        private async Task SweepAsync(DateTime now)
        {
            var last = Interlocked.Read(ref _lastSweepTicks);
            if (last != 0 && now.Ticks - last < TimeSpan.FromMinutes(SweepEveryMinutes).Ticks)
                return;

            if (Interlocked.CompareExchange(ref _lastSweepTicks, now.Ticks, last) != last)
                return;

            try
            {
                await _refreshTokenRepo.DeleteSpentAsync(now, now.AddHours(-RevokedRetentionHours));
            }
            catch
            {
                /* never fail a sign-in over cleanup */
            }
        }

        public async Task LogoutAsync(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return;

            var stored = await _refreshTokenRepo.GetByHashAsync(Hash(refreshToken));
            if (stored == null || stored.RevokedAt != null) return;

            stored.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepo.SaveChangesAsync();
        }

        // Issues an access token plus a fresh refresh token, optionally
        // marking the token being replaced as spent in the same save.
        private async Task<LoginResult> IssueAsync(User user, System.Collections.Generic.List<string> roles, RefreshToken? spend = null)
        {
            var now = DateTime.UtcNow;
            // Read beside the roles this was called with: both are resolved
            // here, at the one choke point login and refresh share, so the
            // token carries the whole reach and no request has to look it up.
            var extraDepartments = await _authRepo.GetExtraDepartmentIdsAsync(user.Id);
            var accessToken = _tokenService.GenerateToken(user.Id, user.UserName ?? string.Empty, user.FullName, user.DepartmentId, extraDepartments, roles);

            var raw = NewRawToken();
            var record = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = Hash(raw),
                CreatedAt = now,
                ExpiresAt = now.AddDays(RefreshDays)
            };
            await _refreshTokenRepo.AddAsync(record);

            if (spend != null)
            {
                spend.RevokedAt = now;
                spend.ReplacedByHash = record.TokenHash;
            }

            await _refreshTokenRepo.SaveChangesAsync();

            // Opportunistic housekeeping: this host runs no scheduled jobs,
            // so old rows are swept here.
            await SweepAsync(now);

            return LoginResult.Success(
                accessToken,
                raw,
                AccessMinutes * 60,
                RefreshDays * 24 * 60 * 60);
        }

        private static string NewRawToken() =>
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        private static string Hash(string raw) =>
            Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));
    }
}
