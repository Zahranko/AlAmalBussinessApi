using AlAmalBusiness.Application.DTOs.Attendance;
using AlAmalBusiness.Application.Services.Interface.Attendance;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Attendance.Controllers
{
    // Admin maintenance of the attendance codes — the letters the grid's
    // dropdowns offer and the hours each is worth. Answers the saved row on
    // success and { message } with 400 (rule broken) or 404 (no such row).
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = nameof(AppRoles.Admin))]
    public class AttendanceListsController : ControllerBase
    {
        private readonly IAttendanceCodeService _codes;

        public AttendanceListsController(IAttendanceCodeService codes)
        {
            _codes = codes;
        }

        [HttpGet("codes")]
        public async Task<IActionResult> GetCodes() => Ok(await _codes.GetAllAsync());

        [HttpPost("codes")]
        public Task<IActionResult> CreateCode(AttendanceCodeDTO dto) =>
            Run(() => _codes.CreateAsync(dto));

        [HttpPut("codes/{id:int}")]
        public Task<IActionResult> UpdateCode(int id, AttendanceCodeDTO dto) =>
            Run(() => _codes.UpdateAsync(id, dto));

        private async Task<IActionResult> Run<T>(Func<Task<AttendanceResult<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound(new { message = result.Error });
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Item);
        }
    }
}
