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

      

        public async Task<(User? User, string? Error)> LogInAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
                return (null, "User or Password is Incorrect");

            if (!user.IsActive)
                return (null, "Your account is inactive. Please contact support.");

            return (user, null);
        }

        public async Task<IEnumerable<string>> GetRolesAsync(string userName)
        {
            var user=await _userManager.FindByNameAsync(userName);
            return await _userManager.GetRolesAsync(user!);
        }
    }
}
