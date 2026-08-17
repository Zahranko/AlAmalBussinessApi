using AlAmalBusiness.Application.DTOs.Auth;
using AlAmalBusiness.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlAmalBusiness.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("PerUserLimit")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authServices;
    public AuthController(IAuthService authServices)
    {
        _authServices = authServices;
    }
    [HttpPost("login")]
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

}