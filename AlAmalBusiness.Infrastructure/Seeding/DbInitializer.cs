using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Infrastructure.Seeding
{
    public class DbInitializer
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        public DbInitializer(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }
        public async Task SeedRolesAsync()
        {
            string[] Roles = new string[]
            {
                AppRoles.Admin,
                AppRoles.CManager,
                AppRoles.CEmployee,
                AppRoles.CUser,
                AppRoles.FManager,
                AppRoles.FEmployee,
                AppRoles.FUser
            };
            foreach (var role in Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}





