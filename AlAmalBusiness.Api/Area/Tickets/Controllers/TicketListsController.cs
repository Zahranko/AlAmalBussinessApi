using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Controllers
{
    // Admin maintenance of the ticket categories, procedures and reasons —
    // the same plain REST shape as AppointmentListsController. Answers the
    // saved row on success and { message } with 400 (rule broken) or 404
    // (no such row) otherwise. Staff read the active entries through
    // TicketController's options endpoint, not from here.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = nameof(AppRoles.Admin))]
    public class TicketListsController : ControllerBase
    {
        private readonly ITicketListService _lists;

        public TicketListsController(ITicketListService lists)
        {
            _lists = lists;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories() => Ok(await _lists.GetCategoriesAsync());

        [HttpPost("categories")]
        public Task<IActionResult> CreateCategory(TicketListItemDTO dto) => Run(() => _lists.CreateCategoryAsync(dto));

        [HttpPut("categories/{id:int}")]
        public Task<IActionResult> UpdateCategory(int id, TicketListItemDTO dto) => Run(() => _lists.UpdateCategoryAsync(id, dto));

        [HttpGet("procedures")]
        public async Task<IActionResult> GetProcedures() => Ok(await _lists.GetProceduresAsync());

        [HttpPost("procedures")]
        public Task<IActionResult> CreateProcedure(TicketListItemDTO dto) => Run(() => _lists.CreateProcedureAsync(dto));

        [HttpPut("procedures/{id:int}")]
        public Task<IActionResult> UpdateProcedure(int id, TicketListItemDTO dto) => Run(() => _lists.UpdateProcedureAsync(id, dto));

        [HttpGet("reasons")]
        public async Task<IActionResult> GetReasons() => Ok(await _lists.GetReasonsAsync());

        [HttpPost("reasons")]
        public Task<IActionResult> CreateReason(TicketReasonItemDTO dto) => Run(() => _lists.CreateReasonAsync(dto));

        [HttpPut("reasons/{id:int}")]
        public Task<IActionResult> UpdateReason(int id, TicketReasonItemDTO dto) => Run(() => _lists.UpdateReasonAsync(id, dto));

        private async Task<IActionResult> Run<T>(Func<Task<TicketListResponse<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound(new { message = result.Message });
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result.Item);
        }
    }
}
