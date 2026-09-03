using AlAmalBusiness.Application.DTOs.CRM.Stats;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AlAmalBusiness.Api.Area.CRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = HospitalManagerController.CanView)]
    public class HospitalManagerController : ControllerBase
    {
        // No dedicated "Hospital Manager" role exists in AppRoles yet — gated to
        // Admin + CManager for now; adjust if a specific role is added later.
        private const string CanView = nameof(AppRoles.Admin) + "," + nameof(AppRoles.CManager);
        private const string FilterEndpointKey = "hospital-manager-stats";

        private readonly ILeadService _leadService;
        private readonly ILeadExcelReportService _excelReportService;
        private readonly IFilterCacheService _filterCache;

        public HospitalManagerController(ILeadService leadService, ILeadExcelReportService excelReportService, IFilterCacheService filterCache)
        {
            _leadService = leadService;
            _excelReportService = excelReportService;
            _filterCache = filterCache;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        [HttpGet("stats")]
        public async Task<ActionResult<HospitalManagerStatsDTO>> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var filter = await ResolveDateRangeAsync(from, to);
            return Ok(await _leadService.GetHospitalManagerStatsAsync(filter.From, filter.To));
        }

        [HttpGet("stats/export")]
        public async Task<IActionResult> ExportStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var filter = await ResolveDateRangeAsync(from, to);
            var stats = await _leadService.GetHospitalManagerStatsAsync(filter.From, filter.To);
            var bytes = _excelReportService.Build(stats);
            var fileName = $"hospital-report-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // A bare request (no query string) restores the caller's last date
        // range for this endpoint; any query string present is used exactly
        // as sent and becomes the new "last filter" for next time.
        private async Task<HospitalManagerFilterCacheDTO> ResolveDateRangeAsync(DateTime? from, DateTime? to)
        {
            if (!Request.QueryString.HasValue)
            {
                var cached = await _filterCache.GetFilterAsync<HospitalManagerFilterCacheDTO>(CurrentUserId, FilterEndpointKey);
                if (cached != null) return cached;
            }

            var filter = new HospitalManagerFilterCacheDTO { From = from, To = to };
            await _filterCache.SaveFilterAsync(CurrentUserId, FilterEndpointKey, filter);
            return filter;
        }

        [HttpGet("doctors/{doctorId:int}/export")]
        public async Task<IActionResult> ExportDoctorLeads(int doctorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var export = await _leadService.GetDoctorLeadExportAsync(doctorId, from, to);
            if (export is null)
                return NotFound();

            var bytes = _excelReportService.BuildDoctorLeads(export);
            var safeName = string.Concat(export.DoctorName.Where(char.IsLetterOrDigit));
            var fileName = $"doctor-leads-{safeName}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
