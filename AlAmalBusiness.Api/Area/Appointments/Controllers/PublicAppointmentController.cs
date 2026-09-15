using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Appointments.Controllers
{
    // The public appointment page (appointment.<domain>) — anonymous, like
    // PublicFeedbackController, and kept to exactly what a patient's browser
    // needs: the two pickers and the submit. Nothing here reads a stored
    // request back. Shares the tight "PublicFormLimit" (10/min per IP).
    [ApiController]
    [Route("api/public/appointment")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicFormLimit")]
    public class PublicAppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public PublicAppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        // Active procedures and referral sources, id + name only.
        [HttpGet("options")]
        public async Task<IActionResult> GetOptions() =>
            Ok(await _appointmentService.GetFormOptionsAsync());

        [HttpPost]
        public async Task<IActionResult> Submit(CreateAppointmentRequestDTO request)
        {
            var context = new SubmissionContext(
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            var result = await _appointmentService.SubmitAsync(request, context);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
