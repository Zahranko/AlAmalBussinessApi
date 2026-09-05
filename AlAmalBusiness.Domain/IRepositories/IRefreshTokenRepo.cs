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

        // Housekeeping: drop rows that expired a while ago, so the table
        // doesn't grow without bound on a host with no scheduled jobs.
        Task<int> DeleteExpiredBeforeAsync(System.DateTime cutoffUtc);
    }
}
