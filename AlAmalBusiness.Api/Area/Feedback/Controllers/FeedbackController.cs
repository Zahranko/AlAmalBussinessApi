using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Feedback.Response;
using AlAmalBusiness.Application.Services.Interface.Feedback;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Feedback.Controllers
{
    // The staff inbox for patient feedback. Patients never reach this
    // controller — they submit through PublicFeedbackController, which is the
    // one anonymous surface in the area, kept separate so [AllowAnonymous] can
    // never leak onto a read endpoint by accident.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = FeedbackController.FeedbackAccess)]
    public class FeedbackController : ControllerBase
    {
        // The feedback site's own roles: a manager reads every department's
        // messages, an employee only their own department's, and an FUser
        // reads without changing anything. These are the F* roles the rest of
        // the product uses — the FB* trio this controller first shipped with
        // was a second spelling of the same three, and is gone from AppRoles.
        private const string FeedbackAccess = nameof(AppRoles.FManager) + "," + nameof(AppRoles.FEmployee) + "," + nameof(AppRoles.FUser) + "," + nameof(AppRoles.Admin);
        private const string CanWork = nameof(AppRoles.FManager) + "," + nameof(AppRoles.FEmployee) + "," + nameof(AppRoles.Admin);
        // The dashboard is a supervisor's screen: an employee works the
        // queue, a manager reads how the queue is going.
        private const string CanReport = nameof(AppRoles.FManager) + "," + nameof(AppRoles.Admin);

        private readonly IFeedbackService _feedbackService;
        private readonly IFeedbackExcelReportService _excelReportService;

        public FeedbackController(IFeedbackService feedbackService, IFeedbackExcelReportService excelReportService)
        {
            _feedbackService = feedbackService;
            _excelReportService = excelReportService;
        }

        // Built from the caller's own validated token, never from the request
        // body — the department id is what scopes the inbox, so it has to come
        // from something the caller can't rewrite.
        private FeedbackActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new FeedbackActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    // Admin alone is unrestricted — a manager's reach is their
                    // own department (see FeedbackService.VisibleDepartment).
                    User.IsInRole(nameof(AppRoles.Admin)),
                    departmentId);
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResultDTO<FeedbackListItemResponse>>> GetPaged(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            FeedbackType? type = null,
            FeedbackStatus? status = null,
            int? departmentId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            var query = new FeedbackListQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Type = type,
                Status = status,
                DepartmentId = departmentId,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(await _feedbackService.GetPagedAsync(query, Actor));
        }

        // The dashboard. A manager gets their own department's numbers; an
        // admin gets every department's, and may narrow to one.
        [HttpGet("stats")]
        [Authorize(Roles = CanReport)]
        public async Task<ActionResult<FeedbackStatsResponse>> GetStats(
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            int? departmentId = null)
        {
            var query = new FeedbackStatsQuery
            {
                From = fromDate,
                To = toDate,
                DepartmentId = departmentId
            };

            return Ok(await _feedbackService.GetStatsAsync(query, Actor));
        }

        // The same numbers as the dashboard, as a workbook. It goes back
        // through GetStatsAsync rather than taking anything on trust, so a
        // manager exporting gets the one department their token allows and an
        // admin gets every department — the export can't widen what the
        // screen already scoped.
        [HttpGet("stats/export")]
        [Authorize(Roles = CanReport)]
        public async Task<IActionResult> ExportStats(
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            int? departmentId = null)
        {
            var query = new FeedbackStatsQuery
            {
                From = fromDate,
                To = toDate,
                DepartmentId = departmentId
            };

            var stats = await _feedbackService.GetStatsAsync(query, Actor);
            var bytes = _excelReportService.Build(stats);
            var fileName = $"feedback-report-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var feedback = await _feedbackService.GetDetailAsync(id, Actor);
            return feedback == null ? NotFound() : Ok(feedback);
        }

        // Staff-side lookup of the code the patient was given on submit.
        [HttpGet("reference/{reference}")]
        public async Task<IActionResult> GetByReference(string reference)
        {
            var feedback = await _feedbackService.GetByReferenceAsync(reference, Actor);
            return feedback == null ? NotFound() : Ok(feedback);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> SetStatus(int id, UpdateFeedbackStatusDTO request) =>
            Run(() => _feedbackService.ChangeStatusAsync(id, request, Actor));

        [HttpPatch("{id:int}/assignee")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> Assign(int id, AssignFeedbackDTO request) =>
            Run(() => _feedbackService.AssignAsync(id, request, Actor));

        // Moves a misrouted message to the department it belongs to.
        //
        // Answers 204 rather than the updated record on purpose: the mover is
        // normally scoped to their own department, so the moment it leaves
        // they can no longer read it. The client goes back to the inbox.
        [HttpPost("{id:int}/forward")]
        [Authorize(Roles = CanWork)]
        public async Task<IActionResult> Forward(int id, ForwardFeedbackDTO request)
        {
            var result = await _feedbackService.ForwardAsync(id, request, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return NoContent();
        }

        // A plain timeline comment, from the box under the history.
        [HttpPost("{id:int}/notes")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> AddNote(int id, AddFeedbackNoteDTO request) =>
            Run(() => _feedbackService.AddNoteAsync(id, request, Actor));

        // 404 for a message the caller may not see (not confirming that the id
        // exists is the point of the scoping), 409 for a rule they broke.
        private async Task<IActionResult> Run(Func<Task<FeedbackActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Feedback);
        }
    }
}
