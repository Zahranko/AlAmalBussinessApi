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
    // Staff support tickets. Raising and solving are different jobs held by
    // different roles (see AppRoles): TEmployee and TManager raise, TSupport
    // and TInsurance solve, and a TManager additionally reads their own
    // department's tickets without being able to act on them.
    //
    // Stacked [Authorize] attributes AND together: the class-level gate is
    // "can enter tickets at all", each action re-declares what it needs. Who
    // may read or comment on *this* ticket is a per-resource check the
    // service makes, answered 404 when it fails.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = TicketAccess)]
    public class TicketController : ControllerBase
    {
        private const string TicketAccess =
            nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.TSupport) + "," +
            nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);
        // Raising a ticket: the two department roles. The agents who solve
        // them don't raise them.
        private const string CanCreate = nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.Admin);
        // Solving — close and reopen — on whichever side the ticket sits.
        // Which side a given ticket is on is the service's call
        // (TicketService.Works), and reopening belongs to the same desk that
        // closed it rather than to a separate seniority tier.
        private const string CanWork = nameof(AppRoles.TSupport) + "," + nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);
        // The dashboard. Admin only — widen to TSupport here if the support
        // agent should read their own numbers.
        private const string CanReport = nameof(AppRoles.Admin);

        private readonly ITicketService _tickets;

        public TicketController(ITicketService tickets)
        {
            _tickets = tickets;
        }

        // Built from the caller's own validated token, never from the request
        // body — the department stamped on a new ticket and every capability
        // below have to come from something the caller can't rewrite.
        private TicketActor Actor
        {
            get
            {
                var isAdmin = User.IsInRole(nameof(AppRoles.Admin));

                // 0 (or no claim) is an account with no department.
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new TicketActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    isAdmin,
                    isAdmin || User.IsInRole(nameof(AppRoles.TSupport)),
                    isAdmin || User.IsInRole(nameof(AppRoles.TInsurance)),
                    isAdmin || User.IsInRole(nameof(AppRoles.TManager)),
                    departmentId > 0 ? departmentId : null);
            }
        }

        // The New ticket form's pickers, in one call.
        [HttpGet("options")]
        [Authorize(Roles = CanCreate)]
        public async Task<ActionResult<TicketFormOptionsResponse>> GetOptions() =>
            Ok(await _tickets.GetFormOptionsAsync());

        [HttpPost]
        [Authorize(Roles = CanCreate)]
        public Task<IActionResult> Create(CreateTicketDTO request) =>
            Run(() => _tickets.CreateAsync(request, Actor));

        // The queue. `scope` is the tab: open (default), mine, unassigned or
        // closed; an explicit status narrows any of them. What it holds is
        // the caller's reach, OR-ed: the support agent every unflagged
        // ticket, the insurance desk the flagged ones, a manager their own
        // department, and everyone their own. No role gate beyond the class
        // one — a TEmployee's queue is simply their own tickets.
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResultDTO<TicketListItemResponse>>> GetPaged(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            TicketStatus? status = null,
            TicketQueueScope scope = TicketQueueScope.Open,
            int? categoryId = null)
        {
            var query = new TicketListQuery { Page = page, PageSize = pageSize, Search = search, Status = status, CategoryId = categoryId };
            return Ok(await _tickets.GetQueueAsync(query, scope, Actor));
        }

        // What the caller raised — a TEmployee's whole world.
        [HttpGet("created-by-me")]
        [Authorize(Roles = CanCreate)]
        public async Task<ActionResult<PagedResultDTO<TicketListItemResponse>>> GetCreatedByMe(
            int page = 1,
            int pageSize = 12,
            string? search = null,
            TicketStatus? status = null,
            int? categoryId = null)
        {
            var query = new TicketListQuery { Page = page, PageSize = pageSize, Search = search, Status = status, CategoryId = categoryId };
            return Ok(await _tickets.GetCreatedByMeAsync(query, Actor));
        }

        // The dashboard: status split, how long tickets wait for a first
        // reply and for a close, and per-person figures. Admin-only. The
        // period bounds when a ticket was RAISED, so one date meaning runs
        // through every number on the screen.
        [HttpGet("stats")]
        [Authorize(Roles = CanReport)]
        public async Task<ActionResult<TicketStatsResponse>> GetStats(DateOnly? from = null, DateOnly? to = null) =>
            Ok(await _tickets.GetStatsAsync(new TicketStatsQuery { From = from, To = to }));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ticket = await _tickets.GetDetailAsync(id, Actor);
            return ticket == null ? NotFound() : Ok(ticket);
        }

        // No role beyond the class gate: whoever works the ticket's side and
        // its own creator may comment, which only the service can tell.
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

        // 404 for a ticket the caller may not see (not confirming that the id
        // exists), 409 for a rule they broke, otherwise the ticket as it now
        // stands.
        private async Task<IActionResult> Run(Func<Task<TicketActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Ticket);
        }
    }
}
