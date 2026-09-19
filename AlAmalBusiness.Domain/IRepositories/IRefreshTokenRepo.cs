using AlAmalBusiness.Domain.Models;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IRefreshTokenRepo
    {
        Task AddAsync(RefreshToken token);

        // Looked up by hash — the raw token is never stored.
        Task<RefreshToken?> GetByHashAsync(string tokenHash);

        Task SaveChangesAsync();

        // Every still-valid token for one user, for "sign out everywhere" and
        // for cutting off a chain once a used token is replayed.
        Task RevokeAllForUserAsync(string userId);

        // Housekeeping, on a host with no scheduled jobs. Two cutoffs,
        // because rotation is what actually fills this table: every refresh
        // spends one row and writes another, so the spent ones outnumber the
        // usable ones by a hundred to one within a fortnight.
        //
        // expiredBeforeUtc drops rows past their ExpiresAt — those are
        // refused by RefreshAsync anyway, so nothing can be lost by removing
        // them. revokedBeforeUtc drops rows spent longer ago than that: they
        // are kept only so a replayed token can be spotted (see the class
        // comment on RefreshToken), and that signal is worth nothing once the
        // value is old enough that no client would still be holding it.
        Task<int> DeleteSpentAsync(System.DateTime expiredBeforeUtc, System.DateTime revokedBeforeUtc);
    }
}
