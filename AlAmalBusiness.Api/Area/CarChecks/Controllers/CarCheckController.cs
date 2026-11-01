using AlAmalBusiness.Application.DTOs.CarChecks;
using AlAmalBusiness.Application.Services.Interface.CarChecks;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.CarChecks.Controllers
{
    // The security team's daily vehicle checks (جدول تشييك سيارة): the cars
    // list, each car's monthly sheets, and each sheet's days.
    //
    // A SecurityManager does everything but delete, change the cars list,
    // reopen a closed month or edit one: those are Admin-only (the first
    // three by attribute, the last in the service).
    //
    // Answers: the saved item on success, { message } with 400 for a broken
    // rule, 409 for a duplicate/blocked action, 404 for no such id.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = CarCheckAccess)]
    public class CarCheckController : ControllerBase
    {
        private const string CarCheckAccess = nameof(AppRoles.SecurityManager) + "," + nameof(AppRoles.Admin);

        private readonly ICarCheckService _checks;

        public CarCheckController(ICarCheckService checks)
        {
            _checks = checks;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        private bool IsAdmin => User.IsInRole(nameof(AppRoles.Admin));

        // ---------- cars ----------

        // Every car, retired ones last, with its sheet count and newest month.
        [HttpGet("cars")]
        public async Task<ActionResult<List<SecurityCarResponse>>> GetCars() => Ok(await _checks.GetCarsAsync());

        [HttpPost("cars")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public Task<IActionResult> CreateCar(SaveSecurityCarDTO dto) => Run(() => _checks.CreateCarAsync(dto));

        [HttpPut("cars/{id:int}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public Task<IActionResult> UpdateCar(int id, SaveSecurityCarDTO dto) => Run(() => _checks.UpdateCarAsync(id, dto));

        // The car and its sheets, newest month on top.
        [HttpGet("cars/{id:int}/sheets")]
        public async Task<IActionResult> GetCarSheets(int id)
        {
            var result = await _checks.GetCarSheetsAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- sheets ----------

        [HttpPost("sheets")]
        public Task<IActionResult> OpenSheet(OpenCarCheckSheetDTO dto) => Run(() => _checks.OpenSheetAsync(dto, UserId));

        // The sheet and one entry per day of its month.
        [HttpGet("sheets/{id:int}")]
        public async Task<IActionResult> GetSheet(int id)
        {
            var grid = await _checks.GetGridAsync(id);
            return grid == null ? NotFound() : Ok(grid);
        }

        // Every changed day in one request; answers the whole saved grid.
        [HttpPut("sheets/{id:int}/days")]
        public Task<IActionResult> SaveDays(int id, List<CarCheckDayDTO> days) => Run(() => _checks.SaveDaysAsync(id, days, UserId, IsAdmin));

        // Any SecurityManager may close a month; only an Admin reopens it.
        [HttpPost("sheets/{id:int}/close")]
        public Task<IActionResult> CloseSheet(int id) => Run(() => _checks.CloseSheetAsync(id, UserId));

        [HttpPost("sheets/{id:int}/reopen")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public Task<IActionResult> ReopenSheet(int id) => Run(() => _checks.ReopenSheetAsync(id));

        // Admin only, and only while no day has been filled.
        [HttpDelete("sheets/{id:int}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> DeleteSheet(int id)
        {
            var result = await _checks.DeleteSheetAsync(id);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });
            return NoContent();
        }

        // Names typed before, for the employee column's autocomplete.
        [HttpGet("employees")]
        public async Task<ActionResult<List<string>>> GetEmployees() => Ok(await _checks.GetEmployeeNamesAsync());

        private async Task<IActionResult> Run<T>(Func<Task<CarCheckResult<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (result.Conflict) return Conflict(new { message = result.Error });
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Item);
        }
    }
}
