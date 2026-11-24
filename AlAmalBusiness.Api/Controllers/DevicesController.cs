using AlAmalBusiness.Application.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AlAmalBusiness.Api.Controllers
{
    // The mobile app's push registration (2026-11-22). Any signed-in user —
    // the token is always filed under the caller from their own access token,
    // never under an id in the body. POST after sign-in and whenever Firebase
    // rotates the token; DELETE on sign-out, while the session still works.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _devices;

        public DevicesController(IDeviceService devices)
        {
            _devices = devices;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public sealed class DeviceTokenDTO
        {
            [Required, StringLength(512, MinimumLength = 20)]
            public string? Token { get; set; }

            [StringLength(10)]
            public string? Platform { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Register(DeviceTokenDTO dto)
        {
            await _devices.RegisterAsync(UserId, dto.Token!, dto.Platform);
            return NoContent();
        }

        // Body, not route: a registration token is long and not URL-shaped.
        [HttpDelete]
        public async Task<IActionResult> Unregister(DeviceTokenDTO dto)
        {
            await _devices.UnregisterAsync(UserId, dto.Token!);
            return NoContent();
        }
    }
}
