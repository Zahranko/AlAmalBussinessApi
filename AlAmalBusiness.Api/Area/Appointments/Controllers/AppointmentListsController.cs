using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Appointments.Controllers
{
    // Admin maintenance of the appointment page's referral-sources list. It
    // answers with the saved row on success and { message } with 400 (rule
    // broken) or 404 (no such row) otherwise.
    //
    // This used to carry two more lists, an appointment-only Procedures list
    // and the addresses the new-request email went to. Both were removed on
    // 2026-09-16: the department the patient picks now routes the request,
    // and the email goes to that department's AManagers instead.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = nameof(AppRoles.Admin))]
    public class AppointmentListsController : ControllerBase
    {
        private readonly IAppointmentListService _lists;

        public AppointmentListsController(IAppointmentListService lists)
        {
            _lists = lists;
        }

        [HttpGet("referral-sources")]
        public async Task<IActionResult> GetReferralSources() => Ok(await _lists.GetReferralSourcesAsync());

        [HttpPost("referral-sources")]
        public Task<IActionResult> CreateReferralSource(AppointmentListItemDTO dto) =>
            Run(() => _lists.CreateReferralSourceAsync(dto));

        [HttpPut("referral-sources/{id:int}")]
        public Task<IActionResult> UpdateReferralSource(int id, AppointmentListItemDTO dto) =>
            Run(() => _lists.UpdateReferralSourceAsync(id, dto));

        private async Task<IActionResult> Run<T>(Func<Task<AppointmentListResponse<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound(new { message = result.Message });
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result.Item);
        }
    }
}
