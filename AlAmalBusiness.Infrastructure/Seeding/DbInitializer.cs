using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

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
            // Runs on every cold start — one SELECT for the existing names
            // rather than one RoleExistsAsync round trip per role.
            var existing = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            foreach (var role in Roles)
            {
                if (!existing.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}





