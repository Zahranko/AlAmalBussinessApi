using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Controllers
{
    // The public maintenance request page (ments.<domain>, added 2026-10-25)
    // — anonymous, like PublicAppointmentController, and kept to exactly what
    // the page needs: the submit. Nothing here reads a stored request back; a
    // submit answers its number and nothing else. A separate controller from
    // MaintenanceTicketController so [AllowAnonymous] can never leak onto a
    // read endpoint. Shares the tight "PublicFormLimit" (10/min per IP).
    [ApiController]
    [Route("api/public/maintenance")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicFormLimit")]
    public class PublicMaintenanceTicketController : ControllerBase
    {
        private readonly IMaintenanceTicketService _tickets;

        public PublicMaintenanceTicketController(IMaintenanceTicketService tickets)
        {
            _tickets = tickets;
        }

        [HttpPost]
        public async Task<IActionResult> Submit(PublicMaintenanceTicketDTO request)
        {
            var context = new SubmissionContext(
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            var result = await _tickets.SubmitPublicAsync(request, context);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
