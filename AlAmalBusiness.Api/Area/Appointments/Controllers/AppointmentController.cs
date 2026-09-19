using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Appointments.Response;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Appointments.Controllers
{
    // The staff inbox for appointment requests. Patients never reach this
    // controller — they submit through PublicAppointmentController, which is
    // the one anonymous surface in the area, kept separate so
    // [AllowAnonymous] can never leak onto a read endpoint by accident.
    //
    // Everything here mirrors FeedbackController: same roles shape, same
    // scoping, same 404-not-403 answer for a request outside the caller's
    // department.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = AppointmentController.AppointmentAccess)]
    public class AppointmentController : ControllerBase
    {
        // The appointment page's own roles: a manager reads their department
        // and its dashboard, an employee works the queue, an AUser reads
        // without changing anything.
        private const string AppointmentAccess = nameof(AppRoles.AManager) + "," + nameof(AppRoles.AEmployee) + "," + nameof(AppRoles.AUser) + "," + nameof(AppRoles.Admin);
        private const string CanWork = nameof(AppRoles.AManager) + "," + nameof(AppRoles.AEmployee) + "," + nameof(AppRoles.Admin);
        // The dashboard is a supervisor's screen: an employee works the
        // queue, a manager reads how the queue is going.
        private const string CanReport = nameof(AppRoles.AManager) + "," + nameof(AppRoles.Admin);

        private readonly IAppointmentService _appointments;

        public AppointmentController(IAppointmentService appointments)
        {
            _appointments = appointments;
        }

        // Built from the caller's own validated token, never from the request
        // body — the department id is what scopes the inbox, so it has to
        // come from something the caller can't rewrite.
        private AppointmentActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new AppointmentActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    // Admin alone is unrestricted — everyone else reads the
                    // departments on their token and nothing more (see
                    // AppointmentService.VisibleDepartments).
                    User.IsInRole(nameof(AppRoles.Admin)),
                    departmentId,
                    AppClaims.ReadableDepartmentIds(
                        User.FindFirstValue(AppClaims.DepartmentIds),
                        departmentId));
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResultDTO<AppointmentListItemResponse>>> GetPaged(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            AppointmentStatus? status = null,
            int? departmentId = null,
            int? referralSourceId = null,
            DateOnly? fromDate = null,
            DateOnly? toDate = null)
        {
            var query = new AppointmentListQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                Status = status,
                DepartmentId = departmentId,
                ReferralSourceId = referralSourceId,
                FromDate = fromDate,
                ToDate = toDate
            };

            return Ok(await _appointments.GetPagedAsync(query, Actor));
        }

        // The dashboard. A manager gets their own department's numbers; an
        // admin gets every department's, and may narrow to one.
        [HttpGet("stats")]
        [Authorize(Roles = CanReport)]
        public async Task<ActionResult<AppointmentStatsResponse>> GetStats(
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            int? departmentId = null)
        {
            var query = new AppointmentStatsQuery
            {
                From = fromDate,
                To = toDate,
                DepartmentId = departmentId
            };

            return Ok(await _appointments.GetStatsAsync(query, Actor));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var appointment = await _appointments.GetDetailAsync(id, Actor);
            return appointment == null ? NotFound() : Ok(appointment);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> SetStatus(int id, UpdateAppointmentStatusDTO request) =>
            Run(() => _appointments.ChangeStatusAsync(id, request, Actor));

        [HttpPatch("{id:int}/assignee")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> Assign(int id, AssignAppointmentDTO request) =>
            Run(() => _appointments.AssignAsync(id, request, Actor));

        // Moves a misrouted request to the department it belongs to.
        //
        // Answers 204 rather than the updated record on purpose: the mover is
        // normally scoped to their own department, so the moment it leaves
        // they can no longer read it. The client goes back to the inbox.
        [HttpPost("{id:int}/forward")]
        [Authorize(Roles = CanWork)]
        public async Task<IActionResult> Forward(int id, ForwardAppointmentDTO request)
        {
            var result = await _appointments.ForwardAsync(id, request, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return NoContent();
        }

        // A plain timeline comment, from the box under the history.
        [HttpPost("{id:int}/notes")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> AddNote(int id, AddAppointmentNoteDTO request) =>
            Run(() => _appointments.AddNoteAsync(id, request, Actor));

        // 404 for a request the caller may not see (not confirming that the
        // id exists is the point of the scoping), 409 for a rule they broke.
        private async Task<IActionResult> Run(Func<Task<AppointmentActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Appointment);
        }
    }
}
