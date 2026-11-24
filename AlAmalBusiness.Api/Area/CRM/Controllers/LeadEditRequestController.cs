using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.LeadEditRequest;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AlAmalBusiness.Api.Area.CRM.Controllers
{
    // "Request edit" (2026-11-22). Anyone who works leads may propose a
    // correction; only an Admin approves or declines it. An employee reads
    // only their own requests — another person's answers 404.
    //
    // Answers: 200 with the request, 404 not found / not yours,
    // 409 { message } for a broken rule (already pending, nothing changed,
    // already decided, a retired lookup).
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = CanWork)]
    public class LeadEditRequestController : ControllerBase
    {
        private const string CanWork = AppRoles.CManager + "," + AppRoles.CEmployee + "," + AppRoles.Admin;
        private const string AdminOnly = AppRoles.Admin;

        private readonly ILeadEditRequestService _service;

        public LeadEditRequestController(ILeadEditRequestService service)
        {
            _service = service;
        }

        private LeadEditActor Actor => new(User.FindFirstValue(ClaimTypes.NameIdentifier)!, User.IsInRole(AppRoles.Admin));

        // POST api/LeadEditRequest/lead/{leadId}
        [HttpPost("lead/{leadId:int}")]
        public Task<ActionResult<LeadEditRequestResponse>> Create(int leadId, CreateLeadEditRequestDTO request) =>
            Run(() => _service.CreateAsync(leadId, request, Actor));

        // status = Pending | Approved | Declined (omit for all). An admin sees
        // everyone's unless mine=true; anyone else always sees only their own.
        [HttpGet]
        public async Task<ActionResult<PagedResultDTO<LeadEditRequestResponse>>> GetPaged(
            string? status = null, bool mine = false, int page = 1, int pageSize = 20)
        {
            LeadEditRequestStatus? parsed = Enum.TryParse<LeadEditRequestStatus>(status, true, out var s) ? s : null;
            return Ok(await _service.GetPagedAsync(parsed, mine, page, pageSize, Actor));
        }

        [HttpGet("pending-count")]
        [Authorize(Roles = AdminOnly)]
        public async Task<ActionResult<object>> PendingCount() =>
            Ok(new { count = await _service.CountPendingAsync() });

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _service.GetAsync(id, Actor);
            return item is null ? NotFound() : Ok(item);
        }

        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = AdminOnly)]
        public Task<ActionResult<LeadEditRequestResponse>> Approve(int id, [FromBody] ReviewLeadEditRequestDTO? body) =>
            Run(() => _service.ApproveAsync(id, body?.Note, Actor));

        [HttpPost("{id:int}/decline")]
        [Authorize(Roles = AdminOnly)]
        public Task<ActionResult<LeadEditRequestResponse>> Decline(int id, [FromBody] ReviewLeadEditRequestDTO? body) =>
            Run(() => _service.DeclineAsync(id, body?.Note, Actor));

        private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
        {
            try
            {
                return Ok(await action());
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
