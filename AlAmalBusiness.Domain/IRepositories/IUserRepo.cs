using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IUserRepo
    {
        Task<IdentityResult> CreateUserAsync(User user, string password);
        Task<IdentityResult> AssignRolesAsync(User user, List<string> roles);
        // One query each, roles included — see UserSummaryRow.
        Task<List<UserSummaryRow>> GetUserSummariesAsync();
        Task<UserSummaryRow?> GetUserSummaryAsync(string id);
        // Id and username of every active user, for pickers.
        Task<List<(string Id, string? UserName)>> GetActiveUserNamesAsync();
        Task<IdentityResult> UpdateRolesAsync(string id, List<string> userRoles);
        Task<IdentityResult> UpdateUserAsync(string id, string userName, string fullName, int departmentId, string? email);
        // Replaces a user's extra readable departments wholesale (see
        // UserDepartment). An empty list clears them, leaving the user with
        // their home department alone.
        Task SetExtraDepartmentsAsync(string userId, IReadOnlyCollection<int> departmentIds);
        // Distinct email addresses of active users in the given role who can
        // read the given department — whether it is the one they work in or
        // one they have been granted — and have an address set. This is who
        // gets told about new feedback or a new appointment request.
        Task<List<string>> GetActiveEmailsInRoleAsync(string role, int departmentId);
        // The same, across every department and any of several roles, leaving
        // one user out — who to tell about a new ticket, minus whoever raised it.
        Task<List<string>> GetActiveEmailsInRolesAsync(IReadOnlyCollection<string> roles, string? excludeUserId);
        // One active user's email, or null when they have none (or are disabled).
        Task<string?> GetActiveEmailAsync(string userId);
        Task<IdentityResult> ResetPasswordAsync(string id, string password);
        Task<IdentityResult> DisableUserAsync(string id);
        Task<IdentityResult> EnableUserAsync(string id);
    }
}
