using AlAmalBusiness.Application.DTOs.Users;
using AlAmalBusiness.Application.DTOs.Users.Response;
using AlAmalBusiness.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface IUserServices
    {
        public Task<CreateUserResult> CreateUserAsync(CreateUserDTO user);
        public Task<List<GetUserResponse>> GetAllUserAsync();
        Task<UpdateUserResponse> UpdateRolesAsync(string id,UpdateUserRolesDTO updateDTO);
        Task<UpdateUserResponse> UpdateUserAsync(string id,UpdateUserDto updateDTO);
        Task<UpdateUserResponse> ResetPasswordAsync(string id,ResetPasswordDTO updateDTO);
        Task<UpdateUserResponse> DisableUserAsync(string id);
        Task<UpdateUserResponse> EnableUserAsync(string id);
        Task<GetUserResponse> GetUserById(string id);
    }
}
