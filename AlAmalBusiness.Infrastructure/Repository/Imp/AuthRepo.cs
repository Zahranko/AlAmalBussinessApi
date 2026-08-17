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

      

        public async Task<(string,bool)> LogInAsync(string userNames, string password)
        {
            var user = await _userManager.FindByNameAsync(userNames);

            if (user != null && await _userManager.CheckPasswordAsync(user, password))
            {

                if (!user.IsActive)
                {
                    return ("Your account is inactive. Please contact support.", false);
                }

            
                    return (user.Id, true);
                
            }
            else
                return ("User or Password os Incorrect",false);
           

        }  
        public async Task<IEnumerable<string>> GetRolesAsync(string userName)
        {
            var user=await _userManager.FindByNameAsync(userName);
            return await _userManager.GetRolesAsync(user!);
        }
    }
}
