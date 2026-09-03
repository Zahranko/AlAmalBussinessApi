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
    public class ClosedReasonsController : ControllerBase
    {
        private readonly IClosedReasonService _closedReasonService;
        public ClosedReasonsController(IClosedReasonService closedReasonService)
        {
            _closedReasonService = closedReasonService;
        }

        [HttpPost("CreateClosedReason")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> CreateClosedReason(ClosedReasonDTO dto)
        {
            var result = await _closedReasonService.CreateClosedReasonAsync(dto);
            return result.Success ? Ok(result.ClosedReason) : BadRequest(result.Message);
        }

        [HttpGet("ClosedReasons")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetAllClosedReasons() =>
            Ok(await _closedReasonService.GetAllClosedReasonsAsync());

        [HttpGet("ActiveClosedReasons")]
        public async Task<IActionResult> GetActiveClosedReasons() =>
            Ok(await _closedReasonService.GetActiveClosedReasonsAsync());

        [HttpGet("ClosedReason/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetClosedReasonById(int id)
        {
            var result = await _closedReasonService.GetClosedReasonByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result.Message);
        }

        [HttpPut("updateClosedReason/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> UpdateClosedReason(int id, ClosedReasonDTO dto)
        {
            var result = await _closedReasonService.UpdateClosedReasonAsync(id, dto);
            return result.Success ? Ok(result.ClosedReason) : BadRequest(result.Message);
        }
    }
}
