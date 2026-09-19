using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.IRepositories
{
    public interface IAuthRepo
    {
        // The authenticated User on success (the caller needs Id/UserName/
        // FullName for the token), or an error message to surface.
        Task<(User? User, string? Error)> LogInAsync(string userName, string password);
        Task<IEnumerable<string>> GetRolesAsync(string userName);

        // For the refresh flow, which has a user id rather than a name and
        // must re-read roles so a change takes effect within 15 minutes.
        Task<User?> FindActiveByIdAsync(string userId);
        Task<IEnumerable<string>> GetRolesByIdAsync(string userId);

        // The extra departments this user may read (UserDepartments), read on
        // the same footing as roles above: both go on the token, so both are
        // re-read on every refresh and a change lands within 15 minutes.
        Task<List<int>> GetExtraDepartmentIdsAsync(string userId);
    }
}
