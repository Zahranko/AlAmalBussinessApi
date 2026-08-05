using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

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
            return await _userManager.AddToRolesAsync(user, roles);
        }

        }

    }

