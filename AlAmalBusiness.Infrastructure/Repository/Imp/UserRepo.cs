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

        public async Task<IEnumerable<User>> GetAllUserAsync()
        {
            return await _userManager.Users.ToListAsync();
        }
        public async Task<IEnumerable<string>> GetRolesAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return await _userManager.GetRolesAsync(user!);
        }
        public async Task<IdentityResult> UpdateRolesAsync(string id, List<string> userRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            var currentRoles = await _userManager.GetRolesAsync(user);
            foreach(var role in userRoles)
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
        public async Task<IdentityResult> UpdateUserAsync(string id, string userName, string fullName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            var lookupUser = await _userManager.FindByNameAsync(userName);
            if (lookupUser != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Username already exists." });
            }
            if (fullName == null)
                return IdentityResult.Failed(new IdentityError { Description = "Full name cannot be null." });

            user.UserName = userName;
            user.FullName = fullName;
            return await _userManager.UpdateAsync(user);

        }
        public async Task<IdentityResult> ResetPasswordAsync(string id,string password)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            if (password == null) return IdentityResult.Failed(new IdentityError { Description = "Password is Empty!" });
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



        }
        
    }


