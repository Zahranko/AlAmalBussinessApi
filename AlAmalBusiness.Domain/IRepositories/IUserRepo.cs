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
        Task<IEnumerable<User>> GetAllUserAsync();
        Task<IEnumerable<string>> GetRolesAsync(string id);
        Task<IdentityResult> UpdateRolesAsync(string id, List<string> userRoles);
        Task<IdentityResult> UpdateUserAsync(string id, string userName, string fullName);
        Task<IdentityResult> ResetPasswordAsync(string id, string password);
        Task<IdentityResult> DisableUserAsync(string id);
        Task<User?> GetUserByIdAsync(string id);
    }
}
