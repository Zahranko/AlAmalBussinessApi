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
    public class ProceduresController : ControllerBase
    {
        private readonly IProcedureService _procedureService;
        public ProceduresController(IProcedureService procedureService)
        {
            _procedureService = procedureService;
        }

        [HttpPost("CreateProcedure")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> CreateProcedure(ProcedureDTO dto)
        {
            var result = await _procedureService.CreateProcedureAsync(dto);
            return result.Success ? Ok(result.Procedure) : BadRequest(result.Message);
        }

        [HttpGet("Procedures")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetAllProcedures() =>
            Ok(await _procedureService.GetAllProceduresAsync());

        [HttpGet("ActiveProcedures")]
        public async Task<IActionResult> GetActiveProcedures() =>
            Ok(await _procedureService.GetActiveProceduresAsync());

        [HttpGet("Procedure/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> GetProcedureById(int id)
        {
            var result = await _procedureService.GetProcedureByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result.Message);
        }

        [HttpPut("updateProcedure/{id}")]
        [Authorize(Roles = nameof(AppRoles.Admin))]
        public async Task<IActionResult> UpdateProcedure(int id, ProcedureDTO dto)
        {
            var result = await _procedureService.UpdateProcedureAsync(id, dto);
            return result.Success ? Ok(result.Procedure) : BadRequest(result.Message);
        }
    }
}
