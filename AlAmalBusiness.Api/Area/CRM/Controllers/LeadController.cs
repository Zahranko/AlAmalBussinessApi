using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.Lead;
using AlAmalBusiness.Application.DTOs.CRM.Lead.Response;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.CRM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AlAmalBusiness.Api.Area.CRM.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("PerUserLimit")]
[Authorize(Roles = LeadController.CrmAccess)]
public class LeadController : ControllerBase
{
    private const string CrmAccess = nameof(AppRoles.CManager) + "," + nameof(AppRoles.CEmployee) + "," + nameof(AppRoles.CUser) + "," + nameof(AppRoles.Admin);
    private const string CanWork = nameof(AppRoles.CManager) + "," + nameof(AppRoles.CEmployee) + "," + nameof(AppRoles.Admin);
    private const string AdminOnly = nameof(AppRoles.Admin);

    private readonly ILeadService _leadService;
    private readonly IFilterCacheService _filterCache;

    public LeadController(ILeadService leadService, IFilterCacheService filterCache)
    {
        _leadService = leadService;
        _filterCache = filterCache;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [Authorize(Roles = CanWork)]
    public Task<ActionResult<CreateLeadResponse>> Create(CreateLeadDTO request) =>
        Run(() => _leadService.CreateLeadAsync(request, CurrentUserId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var detail = await _leadService.GetLeadDetailAsync(id);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet]
    public async Task<ActionResult<List<LeadListItemResponse>>> GetAll(bool excludeCompleted = false) =>
        Ok(await _leadService.GetAllLeadsAsync(excludeCompleted));

    [HttpGet("mine")]
    public async Task<ActionResult<List<LeadListItemResponse>>> GetMine(bool excludeCompleted = false) =>
        Ok(await _leadService.GetMineAsync(CurrentUserId, excludeCompleted));

    [HttpGet("created-by-me")]
    public async Task<ActionResult<List<LeadListItemResponse>>> GetCreatedByMe(bool excludeCompleted = false) =>
        Ok(await _leadService.GetCreatedByMeAsync(CurrentUserId, excludeCompleted));

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultDTO<LeadListItemResponse>>> GetPaged(
        int page = 1, int pageSize = 12, string? search = null, string? status = null, string? scope = null, int? doctorId = null)
    {
        var filter = await ResolveLeadFilterAsync("leads-paged", page, pageSize, search, status, scope, doctorId);
        return Ok(await _leadService.GetPagedAsync(BuildScopedQuery(filter)));
    }

    [HttpGet("created-by-me/paged")]
    public async Task<ActionResult<PagedResultDTO<LeadListItemResponse>>> GetCreatedByMePaged(
        int page = 1, int pageSize = 12, string? search = null, string? status = null, string? scope = null, int? doctorId = null)
    {
        var filter = await ResolveLeadFilterAsync("leads-created-by-me-paged", page, pageSize, search, status, scope, doctorId);
        return Ok(await _leadService.GetCreatedByMePagedAsync(CurrentUserId, BuildScopedQuery(filter)));
    }

    // A bare request (no query string at all) restores the caller's last
    // filter/paging for this endpoint; any query string present is used
    // exactly as sent and becomes the new "last filter" for next time.
    private async Task<LeadFilterCacheDTO> ResolveLeadFilterAsync(
        string endpointKey, int page, int pageSize, string? search, string? status, string? scope, int? doctorId)
    {
        if (!Request.QueryString.HasValue)
        {
            var cached = await _filterCache.GetFilterAsync<LeadFilterCacheDTO>(CurrentUserId, endpointKey);
            if (cached != null) return cached;
        }

        var filter = new LeadFilterCacheDTO { Page = page, PageSize = pageSize, Search = search, Status = status, Scope = scope, DoctorId = doctorId };
        await _filterCache.SaveFilterAsync(CurrentUserId, endpointKey, filter);
        return filter;
    }

    // Translates the dashboard's mutually-exclusive filter chip ("scope") into
    // the repository-level query flags. An explicit status always wins over
    // the open/closed default.
    private LeadListQuery BuildScopedQuery(LeadFilterCacheDTO filter)
    {
        var query = new LeadListQuery { Page = filter.Page, PageSize = filter.PageSize, Search = filter.Search, DoctorId = filter.DoctorId };
        if (Enum.TryParse<LeadStatus>(filter.Status, ignoreCase: true, out var parsedStatus))
            query.Status = parsedStatus;

        switch (filter.Scope)
        {
            case "today": query.TodayOnly = true; break;
            case "mine": query.ClaimedByUserId = CurrentUserId; break;
            case "unclaimed": query.UnclaimedOnly = true; break;
            case "closed": query.OnlyCompleted = true; break;
        }
        query.ExcludeCompletedByDefault = filter.Scope != "closed";

        return query;
    }

    [HttpPost("{id:int}/claim")]
    [Authorize(Roles = CanWork)]
    public Task<ActionResult<LeadActionResponse>> Claim(int id) =>
        Run(() => _leadService.ClaimLeadAsync(id, CurrentUserId));

    [HttpPost("{id:int}/follow-up")]
    [Authorize(Roles = CanWork)]
    public Task<ActionResult<LeadActionResponse>> FollowUp(int id, FollowUpLeadDTO request) =>
        Run(() => _leadService.FollowUpAsync(id, CurrentUserId, request));

    [HttpPost("{id:int}/calls")]
    [Authorize(Roles = CanWork)]
    public Task<ActionResult<LeadActionResponse>> LogCall(int id, LeadCallDTO request) =>
        Run(() => _leadService.LogCallAsync(id, CurrentUserId, request));

    [HttpPost("{id:int}/calls/{callId:int}/done")]
    [Authorize(Roles = CanWork)]
    public Task<ActionResult<LeadActionResponse>> MarkCallDone(int id, int callId) =>
        Run(() => _leadService.MarkCallDoneAsync(id, callId, CurrentUserId));

    [HttpPost("{id:int}/reopen")]
    [Authorize(Roles = AdminOnly)]
    public Task<ActionResult<LeadActionResponse>> Reopen(int id) =>
        Run(() => _leadService.ReopenAsync(id, CurrentUserId));

    // Admin-only: edits every base field of the lead, regardless of status —
    // a data-correction tool, not a workflow action.
    [HttpPut("{id:int}")]
    [Authorize(Roles = AdminOnly)]
    public Task<ActionResult<LeadActionResponse>> Update(int id, AdminUpdateLeadDTO request) =>
        Run(() => _leadService.AdminUpdateLeadAsync(id, CurrentUserId, request));

    // Admin-only case-list filter dropdown — active users.
    [HttpGet("active-users")]
    [Authorize(Roles = AdminOnly)]
    public async Task<ActionResult<List<AssignableUserResponse>>> GetActiveUsers() =>
        Ok(await _leadService.GetActiveUsersAsync());

    private async Task<ActionResult<T>> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
