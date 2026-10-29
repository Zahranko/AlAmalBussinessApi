using AlAmalBusiness.Application.DTOs.Attendance;
using AlAmalBusiness.Application.Services.Interface.Attendance;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Attendance.Controllers
{
    // Monthly attendance sheets: open a month for one or more departments,
    // fill each employee's days with codes, read back totals and shortages.
    // Department-scoped the feedback way — a sheet outside the caller's
    // departments answers 404.
    //
    // Answers: the saved item on success, { message } with 400 for a broken
    // rule, 409 for a duplicate/blocked action, 404 for no such id.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = AttendanceAccess)]
    public class AttendanceController : ControllerBase
    {
        private const string AttendanceAccess = nameof(AppRoles.AttendanceManager) + "," + nameof(AppRoles.Admin);

        private readonly IAttendanceService _attendance;
        private readonly IAttendanceCodeService _codes;

        public AttendanceController(IAttendanceService attendance, IAttendanceCodeService codes)
        {
            _attendance = attendance;
            _codes = codes;
        }

        // From the caller's own validated token, never the body — the
        // department ids are what scope every sheet.
        private AttendanceActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new AttendanceActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    User.IsInRole(nameof(AppRoles.Admin)),
                    departmentId,
                    AppClaims.ReadableDepartmentIds(
                        User.FindFirstValue(AppClaims.DepartmentIds),
                        departmentId));
            }
        }

        // The active codes, for the grid's dropdowns.
        [HttpGet("codes")]
        public async Task<ActionResult<List<AttendanceCodeDTO>>> GetCodes() =>
            Ok(await _codes.GetActiveAsync());

        // ---------- sheets ----------

        [HttpGet("sheets")]
        public async Task<ActionResult<List<AttendanceSheetResponse>>> GetSheets(int? year = null, int? month = null, int? departmentId = null) =>
            Ok(await _attendance.GetSheetsAsync(year, month, departmentId, Actor));

        // One month for several departments, one sheet each, staff carried
        // over from each department's previous sheet. 409 only when every
        // department picked is already open.
        [HttpPost("sheets")]
        public Task<IActionResult> OpenSheets(OpenAttendanceSheetsDTO dto) =>
            Run(() => _attendance.OpenSheetsAsync(dto, Actor));

        // The sheet, its codes and every row with totals — the whole grid.
        [HttpGet("sheets/{id:int}")]
        public async Task<IActionResult> GetSheet(int id)
        {
            var grid = await _attendance.GetGridAsync(id, Actor);
            return grid == null ? NotFound() : Ok(grid);
        }

        // The required hours (208 by default).
        [HttpPut("sheets/{id:int}")]
        public Task<IActionResult> UpdateSheet(int id, UpdateAttendanceSheetDTO dto) =>
            Run(() => _attendance.UpdateSheetAsync(id, dto, Actor));

        // Only while no day has been filled.
        [HttpDelete("sheets/{id:int}")]
        public async Task<IActionResult> DeleteSheet(int id)
        {
            var result = await _attendance.DeleteSheetAsync(id, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });
            return NoContent();
        }

        // The grid's save: every changed row in one request, new ones without
        // an id. Always 200 with one result per row, in order.
        [HttpPost("sheets/{id:int}/rows")]
        public Task<IActionResult> SaveRows(int id, List<SaveAttendanceRowDTO> rows) =>
            Run(() => _attendance.SaveRowsAsync(id, rows, Actor));

        [HttpDelete("rows/{id:int}")]
        public async Task<IActionResult> DeleteRow(int id)
        {
            var result = await _attendance.DeleteRowAsync(id, Actor);
            return result.NotFound ? NotFound() : NoContent();
        }

        private async Task<IActionResult> Run<T>(Func<Task<AttendanceResult<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (result.Conflict) return Conflict(new { message = result.Error });
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Item);
        }
    }
}
