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
    // Staff support tickets, ported from the CRMS Tickets app. Its permission
    // keys became the T* roles (see AppRoles): ticket.create -> TUser,
    // viewAll + workAssign + notifyOnCreate -> TEmployee, TManager adds the
    // reopen the original kept for Admin, and ticket.insurance -> TInsurance,
    // which owns every Insurance ticket outright. ticket.cash was dropped.
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
            nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.TUser) + "," +
            nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);
        // Raising a ticket. The insurance desk alone doesn't raise them.
        private const string CanCreate = nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.TUser) + "," + nameof(AppRoles.Admin);
        // Working either side of the queue: the support team's tickets or the
        // insurance desk's. Which side a given ticket is on is the service's
        // call (TicketService.Works).
        private const string CanWork = nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);
        // A manager reopens a support-queue ticket, the insurance desk its own.
        private const string CanReopen = nameof(AppRoles.TManager) + "," + nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);

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
                    isAdmin || User.IsInRole(nameof(AppRoles.TManager)) || User.IsInRole(nameof(AppRoles.TEmployee)),
                    isAdmin || User.IsInRole(nameof(AppRoles.TManager)),
                    isAdmin || User.IsInRole(nameof(AppRoles.TInsurance)),
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
        // closed; an explicit status narrows any of them. The support team
        // gets every ticket but Insurance ones, the insurance desk gets only
        // those, and anyone on both gets both.
        [HttpGet("paged")]
        [Authorize(Roles = CanWork)]
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

        // What the caller raised — the only list a TUser has.
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
        [Authorize(Roles = CanReopen)]
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
