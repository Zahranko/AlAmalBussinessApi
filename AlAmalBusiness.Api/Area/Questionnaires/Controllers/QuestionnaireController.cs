using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Questionnaires.Controllers
{
    // Staff side of patient questionnaires: build them, and read their
    // results. Patients never reach this controller — they answer through
    // PublicQuestionnaireController, kept separate so [AllowAnonymous] can
    // never leak onto a read endpoint.
    //
    // Two roles, one rule set: an Admin works every department's
    // questionnaires, a QManager only their own department's (see
    // QuestionnaireService.VisibleDepartment) — and anything outside that
    // answers 404, not 403.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = QuestionnaireAccess)]
    public class QuestionnaireController : ControllerBase
    {
        private const string QuestionnaireAccess = nameof(AppRoles.Admin) + "," + nameof(AppRoles.QManager);

        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IQuestionnaireService _questionnaireService;
        private readonly IQuestionnaireExcelReportService _excelReportService;
        private readonly IQuestionnaireMonthlyReportService _monthlyReportService;

        public QuestionnaireController(
            IQuestionnaireService questionnaireService,
            IQuestionnaireExcelReportService excelReportService,
            IQuestionnaireMonthlyReportService monthlyReportService)
        {
            _questionnaireService = questionnaireService;
            _excelReportService = excelReportService;
            _monthlyReportService = monthlyReportService;
        }

        // ---------- monthly report (Admin only) ----------
        //
        // The email the scheduler sends on the 1st, rendered for a month
        // without sending anything. Defaults to last month. It shows every
        // department's copy, with who it would go to — which is why it is
        // Admin-only: a QManager must not see another department's report.
        [HttpGet("monthly-report/preview")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> PreviewMonthlyReport(int? year = null, int? month = null, int? departmentId = null)
        {
            var (y, m) = ResolveMonth(year, month);
            if (y == 0) return BadRequest(new { message = "Invalid month." });
            return Content(await _monthlyReportService.RenderPreviewAsync(y, m, departmentId), "text/html; charset=utf-8");
        }

        // The workbook a month's email attaches for one questionnaire.
        [HttpGet("{id:int}/monthly-report/attachment")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> MonthlyReportAttachment(int id, int? year = null, int? month = null)
        {
            var (y, m) = ResolveMonth(year, month);
            if (y == 0) return BadRequest(new { message = "Invalid month." });
            var file = await _monthlyReportService.BuildAttachmentAsync(id, y, m);
            return file is { } f ? File(f.Content, XlsxContentType, f.FileName) : NotFound();
        }

        // Sends a month's report right now, whether or not the scheduler
        // already did — for a resend, or to try it out.
        [HttpPost("monthly-report/send")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> SendMonthlyReport(int? year = null, int? month = null)
        {
            var (y, m) = ResolveMonth(year, month);
            if (y == 0) return BadRequest(new { message = "Invalid month." });
            return Ok(await _monthlyReportService.SendNowAsync(y, m));
        }

        // Last month unless both are given; (0, 0) for nonsense.
        private static (int Year, int Month) ResolveMonth(int? year, int? month)
        {
            if (year is null || month is null)
            {
                var last = new DateTime(AppClock.Today.Year, AppClock.Today.Month, 1).AddMonths(-1);
                return (last.Year, last.Month);
            }
            return year is >= 2000 and <= 2100 && month is >= 1 and <= 12 ? (year.Value, month.Value) : (0, 0);
        }

        // From the caller's own validated token, never the request body — the
        // department id is what scopes everything here.
        private QuestionnaireActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new QuestionnaireActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    User.IsInRole(nameof(AppRoles.Admin)),
                    departmentId,
                    AppClaims.ReadableDepartmentIds(
                        User.FindFirstValue(AppClaims.DepartmentIds),
                        departmentId));
            }
        }

        // Every questionnaire in scope with its status, response count and
        // average rating over the period (no dates = all time).
        [HttpGet]
        public async Task<ActionResult<QuestionnaireListResponse>> GetAll(DateOnly? fromDate = null, DateOnly? toDate = null) =>
            Ok(await _questionnaireService.GetListAsync(Actor, fromDate, toDate));

        // The list as a workbook. Goes back through GetListAsync, so a QManager's
        // file holds their own department and an admin's holds every one.
        [HttpGet("export")]
        public async Task<IActionResult> ExportList(DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var list = await _questionnaireService.GetListAsync(Actor, fromDate, toDate);
            var fileName = $"questionnaires-{AppClock.Now:yyyyMMdd-HHmm}.xlsx";
            return File(_excelReportService.BuildOverview(list), XlsxContentType, fileName);
        }

        // One questionnaire's results as a workbook: summary, per question, and
        // every response with its ratings, name, phone and notes.
        [HttpGet("{id:int}/export")]
        public async Task<IActionResult> Export(int id, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var data = await _questionnaireService.GetExportDataAsync(id, Actor, fromDate, toDate);
            if (data == null) return NotFound();

            var fileName = $"questionnaire-{data.Stats.Slug}-{AppClock.Now:yyyyMMdd-HHmm}.xlsx";
            return File(_excelReportService.Build(data), XlsxContentType, fileName);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var questionnaire = await _questionnaireService.GetDetailAsync(id, Actor);
            return questionnaire == null ? NotFound() : Ok(questionnaire);
        }

        // Per-question averages and rating distributions.
        [HttpGet("{id:int}/stats")]
        public async Task<IActionResult> GetStats(int id, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var stats = await _questionnaireService.GetStatsAsync(id, Actor, fromDate, toDate);
            return stats == null ? NotFound() : Ok(stats);
        }

        // The individual responses, newest first, with the name/phone a
        // patient chose to leave. contactOnly=true hides the anonymous ones.
        [HttpGet("{id:int}/submissions")]
        public async Task<IActionResult> GetSubmissions(
            int id,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            bool contactOnly = false,
            int page = 1,
            int pageSize = 20)
        {
            var result = await _questionnaireService.GetSubmissionsAsync(id, Actor, fromDate, toDate, contactOnly, page, pageSize);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public Task<IActionResult> Create(SaveQuestionnaireDTO request) =>
            Run(() => _questionnaireService.CreateAsync(request, Actor));

        [HttpPut("{id:int}")]
        public Task<IActionResult> Update(int id, SaveQuestionnaireDTO request) =>
            Run(() => _questionnaireService.UpdateAsync(id, request, Actor));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _questionnaireService.DeleteAsync(id, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return NoContent();
        }

        // 404 for one outside the caller's department, 409 for a rule broken.
        private async Task<IActionResult> Run(Func<Task<QuestionnaireActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Questionnaire);
        }
    }
}
