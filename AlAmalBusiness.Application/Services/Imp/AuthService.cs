using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography;
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
            var accessToken = _tokenService.GenerateToken(user.Id, user.UserName ?? string.Empty, user.FullName, roles);

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

            // Opportunistic housekeeping: this host runs no scheduled jobs, so
            // old rows are swept here. A week past expiry keeps enough history
            // for replay detection to still mean something.
            if (Random.Shared.Next(50) == 0)
            {
                try { await _refreshTokenRepo.DeleteExpiredBeforeAsync(now.AddDays(-RefreshDays)); }
                catch { /* never fail a sign-in over cleanup */ }
            }

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
