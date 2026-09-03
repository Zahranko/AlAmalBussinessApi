using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AlAmalBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("PerUserLimit")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authServices;
    private readonly IUserServices _userServices;
    public AuthController(IAuthService authServices, IUserServices userServices)
    {
        _authServices = authServices;
        _userServices = userServices;
    }
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task <IActionResult> Login(LoginDTO dto)
    {
        if (ModelState.IsValid)
        {
            var res = await _authServices.LoginAsync(dto);
            if (res.IsSuccess == true)
            {
                return Ok(res.Token);
            }
            else
            {
                return Unauthorized(res.Message);
            }
        }
        else
        {
           return BadRequest();
        }
    }

    // Current-user profile — the frontend's session hook reads name/roles from
    // here instead of decoding the JWT client-side. Covered by the global
    // fallback policy (any authenticated user), no explicit [Authorize] needed.
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userServices.GetUserById(userId);
        return user is null ? NotFound() : Ok(user);
    }

}