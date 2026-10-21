using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.DTOs.Tickets.Response;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Controllers
{
    // Maintenance requests (added 2026-10-21) — TicketController's shape
    // with one desk: MRaiser raises, MSupport solves, Admin does both.
    // Stacked [Authorize] attributes AND together; who may read or comment
    // on *this* request is the service's per-resource check, answered 404.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = MaintenanceAccess)]
    public class MaintenanceTicketController : ControllerBase
    {
        private const string MaintenanceAccess =
            nameof(AppRoles.MRaiser) + "," + nameof(AppRoles.MSupport) + "," + nameof(AppRoles.Admin);
        // Raising: the raisers. The team that solves them doesn't raise them.
        private const string CanCreate = nameof(AppRoles.MRaiser) + "," + nameof(AppRoles.Admin);
        // Solving — close and reopen.
        private const string CanWork = nameof(AppRoles.MSupport) + "," + nameof(AppRoles.Admin);

        private readonly IMaintenanceTicketService _tickets;

        public MaintenanceTicketController(IMaintenanceTicketService tickets)
        {
            _tickets = tickets;
        }

        // From the caller's own validated token, never the request body.
        private MaintenanceTicketActor Actor
        {
            get
            {
                var isAdmin = User.IsInRole(nameof(AppRoles.Admin));
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new MaintenanceTicketActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    isAdmin,
                    isAdmin || User.IsInRole(nameof(AppRoles.MSupport)),
                    departmentId > 0 ? departmentId : null);
            }
        }

        [HttpPost]
        [Authorize(Roles = CanCreate)]
        public Task<IActionResult> Create(CreateMaintenanceTicketDTO request) =>
            Run(() => _tickets.CreateAsync(request, Actor));

        // The queue. `scope` is the tab: open (default), mine or closed. The
        // maintenance team sees every request; a raiser only their own.
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResultDTO<MaintenanceTicketListItemResponse>>> GetPaged(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            TicketStatus? status = null,
            MaintenanceQueueScope scope = MaintenanceQueueScope.Open)
        {
            var query = new MaintenanceTicketListQuery { Page = page, PageSize = pageSize, Search = search, Status = status };
            return Ok(await _tickets.GetQueueAsync(query, scope, Actor));
        }

        [HttpGet("created-by-me")]
        [Authorize(Roles = CanCreate)]
        public async Task<ActionResult<PagedResultDTO<MaintenanceTicketListItemResponse>>> GetCreatedByMe(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            TicketStatus? status = null)
        {
            var query = new MaintenanceTicketListQuery { Page = page, PageSize = pageSize, Search = search, Status = status };
            return Ok(await _tickets.GetCreatedByMeAsync(query, Actor));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _tickets.GetDetailAsync(id, Actor);
            return ticket == null ? NotFound() : Ok(ticket);
        }

        // The team and the request's own raiser may comment — the service's call.
        [HttpPost("{id:int}/comments")]
        public Task<IActionResult> Comment(int id, CommentTicketDTO request) =>
            Run(() => _tickets.CommentAsync(id, request, Actor));

        [HttpPost("{id:int}/close")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> Close(int id, CloseTicketDTO request) =>
            Run(() => _tickets.CloseAsync(id, request, Actor));

        [HttpPost("{id:int}/reopen")]
        [Authorize(Roles = CanWork)]
        public Task<IActionResult> Reopen(int id) =>
            Run(() => _tickets.ReopenAsync(id, Actor));

        private async Task<IActionResult> Run(Func<Task<MaintenanceTicketActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Ticket);
        }
    }
}
