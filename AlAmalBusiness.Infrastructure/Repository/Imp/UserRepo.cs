using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.Identity.Client;
using System.Globalization;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{

    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserRepo(AppDbContext context, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task<IdentityResult> CreateUserAsync(User user, string password)
        {
            var lookupUser = await _userManager.FindByNameAsync(user.UserName!);
            if (lookupUser == null)
            {
                return await _userManager.CreateAsync(user, password);
            }
            return IdentityResult.Failed(new IdentityError { Description = "User already exists." });
        }
        public async Task<IdentityResult> AssignRolesAsync(User user, List<string> roles)
        {
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return IdentityResult.Failed(new IdentityError { Description = $"Role '{role}' does not exist." });
                }
            }
            return await _userManager.AddToRolesAsync(user, roles);
        }

        // Roles come back as a correlated collection in the same statement
        // (one LEFT JOIN), instead of FindByIdAsync + GetRolesAsync per user.
        private IQueryable<UserSummaryRow> Summaries() =>
            _context.Users.AsNoTracking().Select(u => new UserSummaryRow
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                Email = u.Email,
                DepartmentId = u.DepartmentId,
                ExtraDepartmentIds = u.ExtraDepartments.Select(x => x.DepartmentId).ToList(),
                IsActive = u.IsActive,
                Roles = (from ur in _context.UserRoles
                         join r in _context.Roles on ur.RoleId equals r.Id
                         where ur.UserId == u.Id
                         select r.Name!).ToList()
            });

        public Task<List<UserSummaryRow>> GetUserSummariesAsync() => Summaries().ToListAsync();

        public Task<UserSummaryRow?> GetUserSummaryAsync(string id) =>
            Summaries().FirstOrDefaultAsync(u => u.Id == id);

        public async Task<List<(string Id, string? UserName)>> GetActiveUserNamesAsync()
        {
            var rows = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync();
            return rows.Select(r => (r.Id, r.UserName)).ToList();
        }
        public async Task<IdentityResult> UpdateRolesAsync(string id, List<string> userRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    return IdentityResult.Failed(new IdentityError { Description = $"Role '{role}' does not exist." });
                }
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles.Except(userRoles));
            if (!removeResult.Succeeded) return removeResult;

            return await _userManager.AddToRolesAsync(user, userRoles.Except(currentRoles));
        }
        public async Task<List<string>> GetActiveEmailsInRoleAsync(string role, int departmentId)
        {
            // One query straight to the Identity tables — UserManager's
            // GetUsersInRoleAsync would pull every user in the role (password
            // hashes included) just to read one column.
            //
            // Someone overseeing several departments is notified about all of
            // them, so the grant table counts here exactly as the home column
            // does: whoever can read the message is whoever hears about it.
            return await (
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                join r in _context.Roles on ur.RoleId equals r.Id
                where r.Name == role
                    && u.IsActive
                    && (u.DepartmentId == departmentId
                        || u.ExtraDepartments.Any(x => x.DepartmentId == departmentId))
                    && u.Email != null && u.Email != ""
                select u.Email!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<string>> GetActiveEmailsInRolesAsync(IReadOnlyCollection<string> roles, string? excludeUserId)
        {
            // Same single join as above, without the department.
            return await (
                from u in _context.Users
                join ur in _context.UserRoles on u.Id equals ur.UserId
                join r in _context.Roles on ur.RoleId equals r.Id
                where roles.Contains(r.Name!)
                    && u.IsActive
                    && u.Id != excludeUserId
                    && u.Email != null && u.Email != ""
                select u.Email!)
                .Distinct()
                .ToListAsync();
        }

        public Task<string?> GetActiveEmailAsync(string userId) =>
            _context.Users
                .Where(u => u.Id == userId && u.IsActive && u.Email != null && u.Email != "")
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

        public async Task<IdentityResult> UpdateUserAsync(string id, string userName, string fullName, int departmentId, string? email)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            var lookupUser = await _userManager.FindByNameAsync(userName);
            if (lookupUser != null && lookupUser.Id != user.Id)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });
            }
            if (fullName == null)
                return IdentityResult.Failed(new IdentityError { Description = "Full name cannot be null." });

            user.UserName = userName;
            user.FullName = fullName;
            user.DepartmentId = departmentId;
            // UpdateAsync re-normalizes NormalizedEmail along with the username.
            user.Email = email;
            return await _userManager.UpdateAsync(user);

        }
        // A full replace, like the rest of an admin's user edit: whatever is
        // sent becomes the complete set of extra departments, and an empty
        // list leaves the user with their home department alone. Rows are
        // compared rather than cleared and re-added so an unchanged save
        // writes nothing.
        public async Task SetExtraDepartmentsAsync(string userId, IReadOnlyCollection<int> departmentIds)
        {
            var current = await _context.UserDepartments
                .Where(x => x.UserId == userId)
                .ToListAsync();

            var wanted = departmentIds.Distinct().ToHashSet();

            var removed = current.Where(x => !wanted.Contains(x.DepartmentId)).ToList();
            if (removed.Count > 0)
                _context.UserDepartments.RemoveRange(removed);

            var existing = current.Select(x => x.DepartmentId).ToHashSet();
            foreach (var id in wanted.Where(d => !existing.Contains(d)))
                _context.UserDepartments.Add(new UserDepartment { UserId = userId, DepartmentId = id });

            await _context.SaveChangesAsync();
        }

        public async Task<IdentityResult> ResetPasswordAsync(string id, string password)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            if (password == null) return IdentityResult.Failed(new IdentityError { Description = "Password is Empty!" });
            // An admin reset is also how a locked-out user gets back in; saved
            // by AddPasswordAsync below together with the new hash.
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (hasPassword)
            {

                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    return removeResult;
                }
            }
            return await _userManager.AddPasswordAsync(user, password);

        }
        public async Task<IdentityResult> DisableUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            user.IsActive = false;
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> EnableUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            user.IsActive = true;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            return await _userManager.UpdateAsync(user);
        }
    }
    }


