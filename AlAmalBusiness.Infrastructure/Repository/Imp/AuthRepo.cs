using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
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

      

        public async Task<string> LogInAsync(string userNames, string password)
        {
            var user = await _userManager.FindByNameAsync(userNames);
            if (user != null&& await _userManager.CheckPasswordAsync(user,password))
            {
                return user.Id;
            }
            else
                return null;

        }
        public async Task<IEnumerable<string>> GetRolesAsync(string userName)
        {
            var user=await _userManager.FindByNameAsync(userName);
            return await _userManager.GetRolesAsync(user!);
        }

        public async Task<bool> isUserActive(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user!= null&&user.IsActive)
            {
                return true;
            }
                return false;
          
        }
    }
}
