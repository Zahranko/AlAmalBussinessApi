using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.DTOs.Users.Response;
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

       

        public async Task<List<GetUserResponse>> GetAllUserAsync()
        {
           var getUsers = await _userRepo.GetAllUserAsync();
            if (getUsers == null || !getUsers.Any())
            {
                return new List<GetUserResponse>();
            }
            List<GetUserResponse> users = new List<GetUserResponse>();
            
            foreach (var user in getUsers)
            {
                var roles = await _userRepo.GetRolesAsync(user.Id);
                users.Add(new GetUserResponse
                {
                    UserId= user.Id,
                    UserName = user.UserName,
                    FullName = user.FullName,
                    Roles = roles.ToList()
                });
            }
            return users;



        }

        public Task<UpdateUserResponse> ResetPasswordAsync(ResetPasswordDTO updateDTO)
        {
            var resetPassword = _userRepo.ResetPasswordAsync(updateDTO.UserId!, updateDTO.Password!);
            if (resetPassword.Result.Succeeded)
            {
                return Task.FromResult(new UpdateUserResponse { IsSuccess = true, Message = "Password reset successfully." });
            }
            else
            {
                var errors = string.Join(", ", resetPassword.Result.Errors.Select(e => e.Description));
                return Task.FromResult(new UpdateUserResponse { IsSuccess = false, Message = errors });
            }

        }

        public Task<UpdateUserResponse> UpdateRolesAsync(UpdateUserRolesDTO updateDTO)
        {
            var updateRoles = _userRepo.UpdateRolesAsync(updateDTO.UserId!, updateDTO.Roles!);
            if (updateRoles.Result.Succeeded)
            {
                return Task.FromResult(new UpdateUserResponse { IsSuccess = true, Message = "Employee roles updated successfully." });
            }
            else
            {
                var errors = string.Join(", ", updateRoles.Result.Errors.Select(e => e.Description));
                return Task.FromResult(new UpdateUserResponse { IsSuccess = false, Message = errors });
            }
        }

        public async Task<UpdateUserResponse> UpdateUserAsync(UpdateUserDto updateDTO)
        {
            var updateUser= await _userRepo.UpdateUserAsync(updateDTO.UserId!, updateDTO.UserName!, updateDTO.Password!);

            if (updateUser.Succeeded) {
            return new UpdateUserResponse { IsSuccess = true, Message = "Employee updated successfully." };
            }

           
            else {
                var errors = string.Join(", ", updateUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors }; }
        }
        public async Task<UpdateUserResponse> DisableUserAsync(DisableUserDTO dto)
        {
            var disableUser =await  _userRepo.DisableUserAsync(dto.UserId!);
           if (disableUser.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee disabled successfully." };
            }
            else
            {
                var errors = string.Join(", ", disableUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }

        }
    }

}
