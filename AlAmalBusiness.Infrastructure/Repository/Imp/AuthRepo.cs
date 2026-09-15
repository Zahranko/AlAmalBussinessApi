using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Infrastructure.Repository.Imp
{



    public class AuthRepo : IAuthRepo
    {
        private readonly UserManager<User> _userManager;
        public AuthRepo(UserManager<User> userManager)
        {
            _userManager = userManager;
        
        }

      

        private const string IncorrectMessage = "User or Password is Incorrect";
        private const string LockedMessage = "Too many failed sign-in attempts. Please try again in 15 minutes.";

        public async Task<(User? User, string? Error)> LogInAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return (null, IncorrectMessage);

            // Checked before the password, so a locked account can't keep
            // being guessed at. Read from LockoutEnd directly rather than
            // IsLockedOutAsync, which answers "no" for any account whose
            // LockoutEnabled flag is off (accounts imported from CRMS).
            if (IsLockedOut(user))
                return (null, LockedMessage);

            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                // Counts the failure and, at MaxFailedAccessAttempts, sets
                // LockoutEnd and resets the count (Program.cs Lockout options).
                await _userManager.AccessFailedAsync(user);
                return (null, IsLockedOut(user) ? LockedMessage : IncorrectMessage);
            }

            if (user.AccessFailedCount > 0)
                await _userManager.ResetAccessFailedCountAsync(user);

            if (!user.IsActive)
                return (null, "Your account is inactive. Please contact support.");

            return (user, null);
        }

        private static bool IsLockedOut(User user) =>
            user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;

        public async Task<User?> FindActiveByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user is { IsActive: true } ? user : null;
        }

        public async Task<IEnumerable<string>> GetRolesByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user == null ? Array.Empty<string>() : await _userManager.GetRolesAsync(user);
        }

        public async Task<IEnumerable<string>> GetRolesAsync(string userName)
        {
            var user=await _userManager.FindByNameAsync(userName);
            return await _userManager.GetRolesAsync(user!);
        }
    }
}
