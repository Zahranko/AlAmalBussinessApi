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
    // Admin maintenance of the appointment page's three lists: procedures,
    // referral sources, and the addresses that get the new-request email.
    // Every list answers with the saved row on success and { message } with
    // 400 (rule broken) or 404 (no such row) otherwise.
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

        [HttpGet("procedures")]
        public async Task<IActionResult> GetProcedures() => Ok(await _lists.GetProceduresAsync());

        [HttpPost("procedures")]
        public Task<IActionResult> CreateProcedure(AppointmentListItemDTO dto) =>
            Run(() => _lists.CreateProcedureAsync(dto));

        [HttpPut("procedures/{id:int}")]
        public Task<IActionResult> UpdateProcedure(int id, AppointmentListItemDTO dto) =>
            Run(() => _lists.UpdateProcedureAsync(id, dto));

        [HttpGet("referral-sources")]
        public async Task<IActionResult> GetReferralSources() => Ok(await _lists.GetReferralSourcesAsync());

        [HttpPost("referral-sources")]
        public Task<IActionResult> CreateReferralSource(AppointmentListItemDTO dto) =>
            Run(() => _lists.CreateReferralSourceAsync(dto));

        [HttpPut("referral-sources/{id:int}")]
        public Task<IActionResult> UpdateReferralSource(int id, AppointmentListItemDTO dto) =>
            Run(() => _lists.UpdateReferralSourceAsync(id, dto));

        [HttpGet("emails")]
        public async Task<IActionResult> GetEmails() => Ok(await _lists.GetEmailsAsync());

        [HttpPost("emails")]
        public Task<IActionResult> CreateEmail(AppointmentEmailDTO dto) =>
            Run(() => _lists.CreateEmailAsync(dto));

        [HttpPut("emails/{id:int}")]
        public Task<IActionResult> UpdateEmail(int id, AppointmentEmailDTO dto) =>
            Run(() => _lists.UpdateEmailAsync(id, dto));

        private async Task<IActionResult> Run<T>(Func<Task<AppointmentListResponse<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound(new { message = result.Message });
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result.Item);
        }
    }
}
