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
    // Returns JSON: { token, refreshToken, expiresInSeconds,
    // refreshExpiresInSeconds }. It used to return the bare token as
    // text/plain; the refresh token had to travel with it, and a shape is
    // easier to extend than a naked string.
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest();

        var res = await _authServices.LoginAsync(dto);
        return res.IsSuccess ? Ok(TokenPayload(res)) : Unauthorized(new { message = res.Message });
    }

    // Trades a refresh token for a new access token, and a new refresh token
    // in place of the one presented. Anonymous by necessity: the caller's
    // access token has usually expired by the time it gets here, which is the
    // whole reason for the call.
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequestDTO dto)
    {
        var res = await _authServices.RefreshAsync(dto.RefreshToken ?? string.Empty);
        return res.IsSuccess ? Ok(TokenPayload(res)) : Unauthorized(new { message = res.Message });
    }

    // Ends this session server-side so a stolen refresh token dies with the
    // sign-out. Always 200: a client clearing its cookies must not be blocked
    // by an unknown or already-revoked token.
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequestDTO dto)
    {
        await _authServices.LogoutAsync(dto.RefreshToken);
        return Ok(new { message = "Signed out." });
    }

    private static object TokenPayload(LoginResult res) => new
    {
        token = res.Token,
        refreshToken = res.RefreshToken,
        expiresInSeconds = res.ExpiresInSeconds,
        refreshExpiresInSeconds = res.RefreshExpiresInSeconds
    };

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