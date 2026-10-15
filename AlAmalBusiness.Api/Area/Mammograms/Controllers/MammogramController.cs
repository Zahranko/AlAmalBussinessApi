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

        public MammogramController(IMammogramService mammograms)
        {
            _mammograms = mammograms;
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

        // Rows due back between two dates, across every sheet, ordered by
        // follow-up date — the call list today, and the query the WhatsApp
        // reminder will run later (reminderStatus=NotSent&withPhone=true).
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

            return Ok(await _mammograms.GetRecordsAsync(new MammogramRecordQuery
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
