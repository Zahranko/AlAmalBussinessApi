using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Feedback.Controllers
{
    // The ONLY anonymous surface in this app besides login — everything a
    // patient's browser needs and nothing more. Deliberately a separate
    // controller from FeedbackController so [AllowAnonymous] can never leak
    // onto a read endpoint: patients submit here, staff read over there
    // behind the JWT.
    //
    // Both actions are rate limited under their own policy (see Program.cs),
    // because this is the one route reachable without a token by anyone who
    // can route to the host.
    [ApiController]
    [Route("api/public/feedback")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicFormLimit")]
    public class PublicFeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IDepartmentService _departmentService;

        public PublicFeedbackController(IFeedbackService feedbackService, IDepartmentService departmentService)
        {
            _feedbackService = feedbackService;
            _departmentService = departmentService;
        }

        // Fills the form's department dropdown. Active departments only — a
        // retired one stays on old messages but can't be picked again.
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments() =>
            Ok(await _departmentService.GetActiveDepartmentsAsync());

        [HttpPost]
        public async Task<IActionResult> Submit(CreateFeedbackDTO request)
        {
            var context = new SubmissionContext(
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            var result = await _feedbackService.SubmitAsync(request, context);

            // The reference number only, never the stored record.
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
