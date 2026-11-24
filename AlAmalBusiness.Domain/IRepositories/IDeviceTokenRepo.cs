using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IDeviceTokenRepo
    {
        // Adds the token for this user, or moves it to them / touches LastSeenAt.
        Task UpsertAsync(string userId, string token, string? platform);
        // Removes the token only if it belongs to this user (sign-out).
        Task RemoveAsync(string userId, string token);
        Task RemoveTokensAsync(IReadOnlyCollection<string> tokens);

        // Recipient resolution mirrors IUserRepo's email lookups exactly —
        // whoever is emailed about something is whoever gets the push.
        Task<List<string>> GetTokensForUsersAsync(IReadOnlyCollection<string> userIds);
        Task<List<string>> GetTokensInRoleAsync(string role, int departmentId);
        Task<List<string>> GetTokensInRolesAsync(IReadOnlyCollection<string> roles, string? excludeUserId);
    }
}
