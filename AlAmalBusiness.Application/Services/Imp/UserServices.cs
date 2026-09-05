using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.DTOs.Users.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using AlAmalBusiness.Domain.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepo _userRepo;
        private readonly IDepartmentRepo _depRepo;
        public UserServices(IUserRepo userRepo,IDepartmentRepo departmentRepo)
        {
            _userRepo = userRepo;
            _depRepo = departmentRepo;
        }

        public async Task<CreateUserResult> CreateUserAsync(CreateUserDTO user)
        {
            var department = await _depRepo.GetDepartmentByIdAsync(user.DepartmentId);
            if (department == null)
            {
                return new CreateUserResult { IsSuccess = false, Message = "Department not found." };
            }
            var newUser = new User
            {
                UserName = user.UserName,
                FullName = user.FullName,
                DepartmentId = user.DepartmentId,
            };

            var result = await _userRepo.CreateUserAsync(newUser, user.Password!);

            if (result.Succeeded)
            {
                var roleRes = await _userRepo.AssignRolesAsync(newUser, user.Roles);
                if (roleRes.Succeeded)
                {
                    return new CreateUserResult { IsSuccess = true, Message = "Employee created successfully." };
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
                users.Add(await ToResponseAsync(user));
            }
            return users;
        }

        // Shared by every path that hands a User entity back to a caller
        // (list, get-by-id, and the response of a successful update) so
        // DepartmentId/IsActive/Roles are never forgotten on one of them —
        // GetUserResponse.DepartmentId used to be left at its default (0) on
        // every response because nothing here ever set it.
        private async Task<GetUserResponse> ToResponseAsync(User user)
        {
            var roles = await _userRepo.GetRolesAsync(user.Id);
            return new GetUserResponse
            {
                UserId = user.Id,
                UserName = user.UserName,
                FullName = user.FullName,
                DepartmentId = user.DepartmentId,
                IsActive = user.IsActive,
                Roles = roles.ToList()
            };
        }

        public async Task<UpdateUserResponse> ResetPasswordAsync(string id, ResetPasswordDTO updateDTO)
        {
            var resetPassword = await _userRepo.ResetPasswordAsync(id, updateDTO.Password!);

            if (resetPassword.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Password reset successfully." };
            }
            else
            {
                var errors = string.Join(", ", resetPassword.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> UpdateRolesAsync(string id, UpdateUserRolesDTO updateDTO)
        {
            var updateRoles = await _userRepo.UpdateRolesAsync(id, updateDTO.Roles!);

            if (updateRoles.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee roles updated successfully." };
            }
            else
            {
                var errors = string.Join(", ", updateRoles.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> UpdateUserAsync(string id, UpdateUserDto updateDTO)
        {
            var department = await _depRepo.GetDepartmentByIdAsync(updateDTO.DepartmentId);
            if (department == null)
            {
                return new UpdateUserResponse { IsSuccess = false, Message = "Department not found." };
            }

            var updateUser = await _userRepo.UpdateUserAsync(id, updateDTO.UserName!, updateDTO.FullName!, updateDTO.DepartmentId);

            if (updateUser.Succeeded)
            {
                var updatedUser = await _userRepo.GetUserByIdAsync(id);
                return new UpdateUserResponse
                {
                    IsSuccess = true,
                    User = await ToResponseAsync(updatedUser!)
                };
            }
            else
            {
                var errors = string.Join(", ", updateUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<UpdateUserResponse> DisableUserAsync(string id)
        {
            var disableUser = await _userRepo.DisableUserAsync(id);

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

        public async Task<UpdateUserResponse> EnableUserAsync(string id)
        {
            var enableUser = await _userRepo.EnableUserAsync(id);

            if (enableUser.Succeeded)
            {
                return new UpdateUserResponse { IsSuccess = true, Message = "Employee enabled successfully." };
            }
            else
            {
                var errors = string.Join(", ", enableUser.Errors.Select(e => e.Description));
                return new UpdateUserResponse { IsSuccess = false, Message = errors };
            }
        }

        public async Task<GetUserResponse> GetUserById(string id)
        {
            var user = await _userRepo.GetUserByIdAsync(id);
            if (user == null)
            {
                return null!;
            }
            return await ToResponseAsync(user);
        }

      
    }
}