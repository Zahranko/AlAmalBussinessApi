using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepo _userRepo;

        public UserServices(IUserRepo userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<CreateUserResult> CreateUserAsync(CreateUserDTO user)
        {
            var newUser = new User
            {
                UserName = user.UserName,
                FullName = user.FullName,
            };
           var result = await _userRepo.CreateUserAsync(newUser, user.Password!);
            if (result.Succeeded)
            {
                var roleRes = await _userRepo.AssignRolesAsync(newUser, user.Roles);
                if (roleRes.Succeeded)
                {
                  return new CreateUserResult { IsSuccess = true, Message= "Employee created successfully." };
                }
                else
                {
                    var roleErrors = string.Join(", ", roleRes.Errors.Select(e => e.Description));
                    return new CreateUserResult { IsSuccess = false, Message = $"Failed to assign roles: {roleErrors}" };
                }

            }
            
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new CreateUserResult { IsSuccess = false, Message = $"Failed to create employee: {errors}" };

        }
    }
    public class CreateUserResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    
    }
}
