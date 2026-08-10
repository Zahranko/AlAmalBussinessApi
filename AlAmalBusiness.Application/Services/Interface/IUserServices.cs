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
        Task<UpdateUserResponse> UpdateRolesAsync(UpdateUserRolesDTO updateDTO);
        Task<UpdateUserResponse> UpdateUserAsync(UpdateUserDto updateDTO);
        Task<UpdateUserResponse> ResetPasswordAsync(ResetPasswordDTO updateDTO);
        Task<UpdateUserResponse> DisableUserAsync(DisableUserDTO dto);
    }
}
