using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Imp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginDTO loginDto);
    }
}
