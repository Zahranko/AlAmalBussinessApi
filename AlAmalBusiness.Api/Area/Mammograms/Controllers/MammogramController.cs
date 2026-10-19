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
    // The mammogram follow-up register: open one sheet per month, fill its
    // rows, export it in the old Excel layout, and list who is due a
    // follow-up across every sheet. Not department-scoped — it is one
    // radiology team's register.
    //
    // Answers: the saved item on success, { message } with 400 for a broken
    // rule, 409 for a duplicate/blocked action, 404 for no such id.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = MammogramAccess)]
    public class MammogramController : ControllerBase
    {
        private const string MammogramAccess = nameof(AppRoles.MManager) + "," + nameof(AppRoles.Admin);

        private readonly IMammogramService _mammograms;
        private readonly IMammogramReminderService _reminders;

        public MammogramController(IMammogramService mammograms, IMammogramReminderService reminders)
        {
            _mammograms = mammograms;
            _reminders = reminders;
        }

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // ---------- sheets ----------

        [HttpGet("sheets")]
        public async Task<ActionResult<List<MammogramSheetResponse>>> GetSheets() =>
            Ok(await _mammograms.GetSheetsAsync());

        [HttpGet("sheets/{id:int}")]
        public async Task<IActionResult> GetSheet(int id)
        {
            var sheet = await _mammograms.GetSheetAsync(id);
            return sheet == null ? NotFound() : Ok(sheet);
        }

        // Opens the month's sheet. 409 if that month is already open.
        [HttpPost("sheets")]
        public Task<IActionResult> OpenSheet(OpenMammogramSheetDTO dto) =>
            Run(() => _mammograms.OpenSheetAsync(dto, UserId));

        // Empty sheets only (opened for the wrong month).
        [HttpDelete("sheets/{id:int}")]
        public async Task<IActionResult> DeleteSheet(int id)
        {
            var result = await _mammograms.DeleteSheetAsync(id);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });
            return NoContent();
        }

        [HttpGet("sheets/{id:int}/records")]
        public async Task<ActionResult<PagedResultDTO<MammogramRecordResponse>>> GetRecords(
            int id,
            int page = 1,
            int pageSize = 50,
            string? search = null,
            MammogramExamType? examType = null,
            string? tech = null)
        {
            if (await _mammograms.GetSheetAsync(id) == null) return NotFound();

            return Ok(await _mammograms.GetRecordsAsync(new MammogramRecordQuery
            {
                SheetId = id,
                Page = page,
                PageSize = pageSize,
                Search = search,
                ExamType = examType,
                Tech = tech
            }));
        }

        [HttpPost("sheets/{id:int}/records")]
        public Task<IActionResult> AddRecord(int id, SaveMammogramRecordDTO dto) =>
            Run(() => _mammograms.AddRecordAsync(id, dto, UserId));

        // The grid's save: every changed row in one request, new ones without
        // an id. Always 200 with one result per row, in order — a row that
        // breaks a rule comes back with its error and the rest still save.
        [HttpPost("sheets/{id:int}/rows")]
        public Task<IActionResult> SaveRows(int id, List<SaveMammogramRowDTO> rows) =>
            Run(() => _mammograms.SaveRowsAsync(id, rows, UserId));

        [HttpGet("sheets/{id:int}/export")]
        public async Task<IActionResult> ExportSheet(int id)
        {
            var file = await _mammograms.ExportSheetAsync(id);
            if (file == null) return NotFound();

            return File(file.Value.Content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                file.Value.FileName);
        }

        // ---------- records ----------

        [HttpGet("records/{id:int}")]
        public async Task<IActionResult> GetRecord(int id)
        {
            var record = await _mammograms.GetRecordAsync(id);
            return record == null ? NotFound() : Ok(record);
        }

        // Full replace.
        [HttpPut("records/{id:int}")]
        public Task<IActionResult> UpdateRecord(int id, SaveMammogramRecordDTO dto) =>
            Run(() => _mammograms.UpdateRecordAsync(id, dto));

        [HttpDelete("records/{id:int}")]
        public async Task<IActionResult> DeleteRecord(int id)
        {
            var result = await _mammograms.DeleteRecordAsync(id);
            return result.NotFound ? NotFound() : NoContent();
        }

        // Follow-ups due between two dates, across every sheet, ordered by
        // date — the call list, with each one's WhatsApp reminder state
        // (MammogramReminderService sends them). A row with both follow-ups in
        // the range is listed twice; FollowUpSlot says which line is which,
        // and reminderStatus filters on that follow-up's own reminder.
        // Without dates: from today, 30 days ahead.
        [HttpGet("follow-ups")]
        public async Task<ActionResult<PagedResultDTO<MammogramRecordResponse>>> GetFollowUps(
            DateOnly? from = null,
            DateOnly? to = null,
            MammogramReminderStatus? reminderStatus = null,
            bool withPhone = false,
            string? search = null,
            int page = 1,
            int pageSize = 50)
        {
            var today = DateOnly.FromDateTime(AppClock.Today);

            return Ok(await _mammograms.GetFollowUpsAsync(new MammogramRecordQuery
            {
                FollowUpFrom = from ?? today,
                FollowUpTo = to ?? (from ?? today).AddDays(30),
                ReminderStatus = reminderStatus,
                OnlyWithPhone = withPhone,
                Search = search,
                Page = page,
                PageSize = pageSize
            }));
        }

        // TEMPORARY test hook behind the Follow-ups page's "Send now" button
        // (asked for 2026-10-17, to be removed once the daily run is trusted):
        // sends today's due reminders straight away instead of waiting for
        // the 08:00 run, then reads back recent statuses. Admin only.
        [HttpPost("reminders/run")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<ActionResult<MammogramReminderRunResult>> RunReminders()
        {
            try
            {
                var result = await _reminders.SendDueAsync();
                if (!result.Enabled) return BadRequest(new { message = "WhatsApp is not configured (WhatsApp:ApiKey)." });
                result.StatusesUpdated = await _reminders.SyncStatusesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                // A test button for an admin: say what broke instead of the
                // global handler's bare 500, which hides it in production.
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = $"Send now failed: {ex.GetType().Name}: {ex.GetBaseException().Message}" });
            }
        }

        // ---------- report builder ----------

        // Every row visited between two days (inclusive, max five years),
        // for the console's custom reports — it counts in the browser.
        [HttpGet("report/rows")]
        public Task<IActionResult> GetReportRows(DateOnly from, DateOnly to) =>
            Run(() => _mammograms.GetReportRowsAsync(from, to));

        // Saved report definitions, shared by everyone with the role.
        [HttpGet("report/definitions")]
        public async Task<ActionResult<List<MammogramReportResponse>>> GetReports() =>
            Ok(await _mammograms.GetReportsAsync());

        [HttpPost("report/definitions")]
        public Task<IActionResult> CreateReport(SaveMammogramReportDTO dto) =>
            Run(() => _mammograms.SaveReportAsync(null, dto, UserId));

        [HttpPut("report/definitions/{id:int}")]
        public Task<IActionResult> UpdateReport(int id, SaveMammogramReportDTO dto) =>
            Run(() => _mammograms.SaveReportAsync(id, dto, UserId));

        [HttpDelete("report/definitions/{id:int}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var result = await _mammograms.DeleteReportAsync(id);
            return result.NotFound ? NotFound() : NoContent();
        }

        // Tech names already used, for the form's autocomplete.
        [HttpGet("techs")]
        public async Task<ActionResult<List<string>>> GetTechs() => Ok(await _mammograms.GetTechNamesAsync());

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
