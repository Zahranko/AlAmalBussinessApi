using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepo _authRepo;
        private readonly ITokenService _tokenService;
        public AuthService(IAuthRepo authRepo , ITokenService tokenService)
        {
            _authRepo = authRepo;
            _tokenService = tokenService;

        }
        public async Task<LoginResult> LoginAsync(LoginDTO loginDto)
        {
            var user = await _authRepo.LogInAsync(loginDto.UserName!,loginDto.Password!);
            var isActive = await _authRepo.isUserActive(loginDto.UserName!);
            if (user!=null)
            {
                if (isActive)
                {
                    var roles = await _authRepo.GetRolesAsync(loginDto.UserName!);
                    if (roles.Any())
                    {
                        var token = _tokenService.GenerateToken(user, loginDto.UserName!, roles);
                        return LoginResult.Success(token);
                    }
                    else
                    {
                        return LoginResult.Fail("User has no roles assigned.");
                    }
                }
                else
                {
                    return LoginResult.Fail("User is not active.");
                }

            }
            else
            {
                
                return LoginResult.Fail("Username or Password is incorrect");
            }


        }
    }
}
