using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
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
    public class ReferalSourcesController : ControllerBase
    {
        private readonly IReferalSourceService _referalSourceService;
        public ReferalSourcesController(IReferalSourceService referalSourceService)
        {
            _referalSourceService = referalSourceService;
        }

        [HttpPost("CreateReferalSource")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> CreateReferalSource(ReferalSourceDTO dto)
        {
            var result = await _referalSourceService.CreateReferalSourceAsync(dto);
            return result.Success ? Ok(result.ReferalSource) : BadRequest(result.Message);
        }

        [HttpGet("ReferalSources")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetAllReferalSources() =>
            Ok(await _referalSourceService.GetAllReferalSourcesAsync());

        [HttpGet("ActiveReferalSources")]
        public async Task<IActionResult> GetActiveReferalSources() =>
            Ok(await _referalSourceService.GetActiveReferalSourcesAsync());

        [HttpGet("ReferalSource/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetReferalSourceById(int id)
        {
            var result = await _referalSourceService.GetReferalSourceByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result.Message);
        }

        [HttpPut("updateReferalSource/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> UpdateReferalSource(int id, ReferalSourceDTO dto)
        {
            var result = await _referalSourceService.UpdateReferalSourceAsync(id, dto);
            return result.Success ? Ok(result.ReferalSource) : BadRequest(result.Message);
        }
    }
}
