using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Domain.IRepositories;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContext;
        public AuthService(IAuthRepo authRepo , ITokenService tokenService, IHttpContextAccessor httpContext)
        {
            _authRepo = authRepo;
            _tokenService = tokenService;
            _httpContext = httpContext;
        }
        public async Task<LoginResult> LoginAsync(LoginDTO loginDto)
        {
            var (user, error) = await _authRepo.LogInAsync(loginDto.UserName!, loginDto.Password!);
            if (user == null)
                return LoginResult.Fail(error ?? "User or Password is Incorrect");

            var roles = await _authRepo.GetRolesAsync(loginDto.UserName!);
            if (!roles.Any())
                return LoginResult.Fail("User has no roles assigned.");

            var token = _tokenService.GenerateToken(user.Id, user.UserName ?? loginDto.UserName!, user.FullName, roles);
            return LoginResult.Success(token);
        }
    }
}
