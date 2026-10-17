using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Mammograms;
using AlAmalBusiness.Application.Services.Interface.Mammograms;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Mammograms.Controllers
{
    // The mammogram day calendar (added 2026-10-17): book a patient into a
    // room's slot, follow the appointment up, mark the patient arrived, and
    // read the month's numbers. Same gate as the register — one radiology
    // team, no department scoping.
    //
    // Answers: the appointment's detail on success, { message } with 400 for
    // a broken rule, 409 for a taken slot or a step out of order, 404 for no
    // such id.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = MammogramAccess)]
    public class MammogramAppointmentController : ControllerBase
    {
        private const string MammogramAccess = nameof(AppRoles.MManager) + "," + nameof(AppRoles.Admin);

        private readonly IMammogramAppointmentService _appointments;

        public MammogramAppointmentController(IMammogramAppointmentService appointments)
        {
            _appointments = appointments;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // view = Today (default) | Open | CreatedToday | Mine | All.
        [HttpGet]
        public async Task<ActionResult<PagedResultDTO<MammogramAppointmentResponse>>> GetPaged(
            MammogramAppointmentView view = MammogramAppointmentView.Today,
            MammogramAppointmentStatus? status = null,
            string? search = null,
            int page = 1,
            int pageSize = 50) =>
            Ok(await _appointments.GetPagedAsync(new MammogramAppointmentListQueryDTO
            {
                View = view,
                Status = status,
                Search = search,
                Page = page,
                PageSize = pageSize
            }, UserId));

        // One day's appointments for the calendar; today without a date.
        [HttpGet("calendar")]
        public async Task<ActionResult<List<MammogramAppointmentResponse>>> GetDay(DateOnly? date = null) =>
            Ok(await _appointments.GetDayAsync(date ?? DateOnly.FromDateTime(AppClock.Today)));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var detail = await _appointments.GetDetailAsync(id);
            return detail == null ? NotFound() : Ok(detail);
        }

        [HttpPost]
        public Task<IActionResult> Book(BookMammogramAppointmentDTO dto) =>
            Run(() => _appointments.BookAsync(dto, UserId));

        [HttpPost("{id:int}/follow-up")]
        public Task<IActionResult> FollowUp(int id, MammogramAppointmentFollowUpDTO dto) =>
            Run(() => _appointments.FollowUpAsync(id, dto, UserId));

        [HttpPost("{id:int}/arrived")]
        public Task<IActionResult> Arrived(int id) =>
            Run(() => _appointments.ArriveAsync(id, UserId));

        // The month's numbers, by the day appointments were booked; this
        // month without parameters.
        [HttpGet("stats")]
        public async Task<ActionResult<MammogramAppointmentStatsResponse>> GetStats(int? year = null, int? month = null) =>
            Ok(await _appointments.GetStatsAsync(year, month));

        // Appointments booked between two dates, both inclusive, as .xlsx.
        [HttpGet("export")]
        public async Task<IActionResult> Export(DateOnly? from = null, DateOnly? to = null)
        {
            var result = await _appointments.ExportAsync(from, to);
            if (!result.Success) return BadRequest(new { message = result.Error });

            return File(result.Item.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.Item.FileName);
        }

        // Doctor names already used, for the booking form's autocomplete.
        [HttpGet("doctors")]
        public async Task<ActionResult<List<string>>> GetDoctors() => Ok(await _appointments.GetDoctorNamesAsync());

        private async Task<IActionResult> Run<T>(Func<Task<MammogramResult<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (result.Conflict) return Conflict(new { message = result.Error });
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Item);
        }
    }
}
