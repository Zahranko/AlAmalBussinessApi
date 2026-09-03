using AlAmalBusiness.Application.DTOs.CRM.Stats;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AlAmalBusiness.Api.Area.CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    // The shared dashboard (KPIs, per-employee chart) is visible to every CRM
    // role; the deeper all-employee breakdown in GetStats stays Admin-only.
    [Authorize(Roles = LeadStatsController.CrmAccess)]
    public class LeadStatsController : ControllerBase
    {
        private const string CrmAccess = nameof(AppRoles.CManager) + "," + nameof(AppRoles.CEmployee) + "," + nameof(AppRoles.CUser) + "," + nameof(AppRoles.Admin);

        private readonly ILeadService _leadService;
        public LeadStatsController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet("stats")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<ActionResult<AdminStatsDTO>> GetStats() =>
            Ok(await _leadService.GetStatsAsync());

        // Dashboard KPI tiles: leads/successes this month vs today, each with a
        // 7-day trend and a delta against the immediately preceding period.
        [HttpGet("kpis")]
        public async Task<ActionResult<DashboardKpiDTO>> GetKpis() =>
            Ok(await _leadService.GetDashboardKpisAsync());

        // "Cases per employee" chart data. period=today|month (default month).
        [HttpGet("employee-cases")]
        public async Task<ActionResult<List<EmployeeCaseCountDTO>>> GetEmployeeCases(string period = "month") =>
            Ok(await _leadService.GetEmployeeCaseCountsAsync(period));

        // Dashboard status band — all-time count per LeadStatus.
        [HttpGet("status-counts")]
        public async Task<ActionResult<LeadStatusBandDTO>> GetStatusCounts() =>
            Ok(await _leadService.GetStatusBandAsync());

        // Dashboard "Lead sources" card.
        [HttpGet("sources")]
        public async Task<ActionResult<List<ReferralSourceStatDTO>>> GetSources() =>
            Ok(await _leadService.GetLeadSourcesAsync());
    }
}
