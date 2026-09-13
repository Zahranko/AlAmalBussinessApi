using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Questionnaires.Controllers
{
    // The anonymous side of questionnaires — what the public page at
    // <public site>/{slug} needs, and nothing more. Separate from
    // QuestionnaireController so [AllowAnonymous] can never reach a staff
    // endpoint, and under the same tight "PublicFormLimit" policy as the
    // public feedback form.
    [ApiController]
    [Route("api/public/questionnaire")]
    [AllowAnonymous]
    [EnableRateLimiting("PublicFormLimit")]
    public class PublicQuestionnaireController : ControllerBase
    {
        private readonly IQuestionnaireService _questionnaireService;

        public PublicQuestionnaireController(IQuestionnaireService questionnaireService)
        {
            _questionnaireService = questionnaireService;
        }

        // The title and questions. 404 for an unknown or inactive slug — the
        // page can't tell the two apart, by design.
        [HttpGet("{slug}")]
        public async Task<IActionResult> Get(string slug)
        {
            var questionnaire = await _questionnaireService.GetPublicAsync(slug);
            return questionnaire == null ? NotFound(new { message = "هذا الاستبيان غير متاح حالياً" }) : Ok(questionnaire);
        }

        [HttpPost("{slug}")]
        public async Task<IActionResult> Submit(string slug, SubmitQuestionnaireDTO request)
        {
            var context = new SubmissionContext(
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            var result = await _questionnaireService.SubmitAsync(slug, request, context);

            if (result.NotFound) return NotFound(result);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
