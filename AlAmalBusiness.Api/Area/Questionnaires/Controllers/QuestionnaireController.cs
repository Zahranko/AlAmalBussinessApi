using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Questionnaires.Controllers
{
    // Staff side of patient questionnaires: build them, and read their
    // results. Patients never reach this controller — they answer through
    // PublicQuestionnaireController, kept separate so [AllowAnonymous] can
    // never leak onto a read endpoint.
    //
    // Two roles, one rule set: an Admin works every department's
    // questionnaires, a QManager only their own department's (see
    // QuestionnaireService.VisibleDepartment) — and anything outside that
    // answers 404, not 403.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = QuestionnaireAccess)]
    public class QuestionnaireController : ControllerBase
    {
        private const string QuestionnaireAccess = nameof(AppRoles.Admin) + "," + nameof(AppRoles.QManager);

        private readonly IQuestionnaireService _questionnaireService;

        public QuestionnaireController(IQuestionnaireService questionnaireService)
        {
            _questionnaireService = questionnaireService;
        }

        // From the caller's own validated token, never the request body — the
        // department id is what scopes everything here.
        private QuestionnaireActor Actor
        {
            get
            {
                int.TryParse(User.FindFirstValue(AppClaims.DepartmentId), out var departmentId);

                return new QuestionnaireActor(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                    User.IsInRole(nameof(AppRoles.Admin)),
                    departmentId);
            }
        }

        // Every questionnaire in scope with its status, response count and
        // average rating over the period (no dates = all time).
        [HttpGet]
        public async Task<ActionResult<QuestionnaireListResponse>> GetAll(DateOnly? fromDate = null, DateOnly? toDate = null) =>
            Ok(await _questionnaireService.GetListAsync(Actor, fromDate, toDate));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var questionnaire = await _questionnaireService.GetDetailAsync(id, Actor);
            return questionnaire == null ? NotFound() : Ok(questionnaire);
        }

        // Per-question averages and rating distributions.
        [HttpGet("{id:int}/stats")]
        public async Task<IActionResult> GetStats(int id, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            var stats = await _questionnaireService.GetStatsAsync(id, Actor, fromDate, toDate);
            return stats == null ? NotFound() : Ok(stats);
        }

        // The individual responses, newest first, with the name/phone a
        // patient chose to leave. contactOnly=true hides the anonymous ones.
        [HttpGet("{id:int}/submissions")]
        public async Task<IActionResult> GetSubmissions(
            int id,
            DateOnly? fromDate = null,
            DateOnly? toDate = null,
            bool contactOnly = false,
            int page = 1,
            int pageSize = 20)
        {
            var result = await _questionnaireService.GetSubmissionsAsync(id, Actor, fromDate, toDate, contactOnly, page, pageSize);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        public Task<IActionResult> Create(SaveQuestionnaireDTO request) =>
            Run(() => _questionnaireService.CreateAsync(request, Actor));

        [HttpPut("{id:int}")]
        public Task<IActionResult> Update(int id, SaveQuestionnaireDTO request) =>
            Run(() => _questionnaireService.UpdateAsync(id, request, Actor));

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _questionnaireService.DeleteAsync(id, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return NoContent();
        }

        // 404 for one outside the caller's department, 409 for a rule broken.
        private async Task<IActionResult> Run(Func<Task<QuestionnaireActionResponse>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });

            return Ok(result.Questionnaire);
        }
    }
}
