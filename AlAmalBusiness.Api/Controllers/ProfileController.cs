using AlAmalBusiness.Application.DTOs.Profile;
using AlAmalBusiness.Application.Services.Interface.Profile;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Controllers
{
    // My profile — every signed-in user, any role (the global fallback policy
    // requires authentication; no role gate on purpose). Each action works on
    // the caller's own account, taken from the token: no route carries an id.
    //
    // Answers: 200 with the result, 400 { message, errors } for a refused
    // change, 404 when the account no longer exists or was disabled.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profile;

        public ProfileController(IProfileService profile)
        {
            _profile = profile;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        private ProfileActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);
                return new ProfileActor(
                    UserId,
                    User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                    AppClaims.ReadableDepartmentIds(User.FindFirstValue(AppClaims.DepartmentIds), departmentId));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ProfileResponse>> Get()
        {
            var profile = await _profile.GetAsync(Actor);
            return profile == null ? NotFound() : Ok(profile);
        }

        // The display name. The new name is in the token from the next refresh.
        [HttpPut("name")]
        public async Task<IActionResult> UpdateName(UpdateProfileNameDTO dto) =>
            Answer(await _profile.UpdateNameAsync(UserId, dto));

        // Current + new password. On success every refresh token of this
        // account is revoked — the caller signs in again with the new password.
        [HttpPost("password")]
        public async Task<IActionResult> ChangePassword(ChangeOwnPasswordDTO dto) =>
            Answer(await _profile.ChangePasswordAsync(UserId, dto));

        // "My status": one section per non-Admin role area the caller holds.
        [HttpGet("stats")]
        public async Task<ActionResult<ProfileStatsResponse>> Stats() => Ok(await _profile.GetStatsAsync(Actor));

        private IActionResult Answer(ProfileResult result)
        {
            if (result.NotFound) return NotFound();
            // `message` carries Identity's reasons too (e.g. "Passwords must
            // have at least one digit"), for clients that read only message.
            if (!result.Success)
                return BadRequest(new
                {
                    message = result.Errors.Count > 0 ? $"{result.Error} {string.Join(" ", result.Errors)}" : result.Error,
                    errors = result.Errors,
                });
            return Ok(new { ok = true });
        }
    }
}
