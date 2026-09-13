using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Feedback;
using AlAmalBusiness.Application.DTOs.Questionnaires;
using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using System;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Questionnaires
{
    public interface IQuestionnaireService
    {
        // Every questionnaire the actor may see, with its response count and
        // average over the period. Admin: all departments. QManager: their own.
        Task<QuestionnaireListResponse> GetListAsync(QuestionnaireActor actor, DateOnly? from, DateOnly? to);

        // Null when it doesn't exist or sits outside the actor's department —
        // answered the same way on purpose.
        Task<QuestionnaireDetailResponse?> GetDetailAsync(int id, QuestionnaireActor actor);

        Task<QuestionnaireStatsResponse?> GetStatsAsync(int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to);

        // The individual responses, newest first, with the patient's optional
        // name/phone. Null when the questionnaire isn't visible to the actor.
        Task<PagedResultDTO<QuestionnaireSubmissionResponse>?> GetSubmissionsAsync(
            int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to, bool contactOnly, int page, int pageSize);

        // The workbook's data for one questionnaire — the same scoping as the
        // results screen (null when not visible), plus each response's ratings.
        Task<QuestionnaireExportData?> GetExportDataAsync(int id, QuestionnaireActor actor, DateOnly? from, DateOnly? to);

        Task<QuestionnaireActionResponse> CreateAsync(SaveQuestionnaireDTO request, QuestionnaireActor actor);

        Task<QuestionnaireActionResponse> UpdateAsync(int id, SaveQuestionnaireDTO request, QuestionnaireActor actor);

        // Refused once patients have answered it — deactivate it instead, so
        // the answers aren't lost with it.
        Task<QuestionnaireActionResponse> DeleteAsync(int id, QuestionnaireActor actor);

        // ---------- anonymous ----------

        Task<PublicQuestionnaireResponse?> GetPublicAsync(string slug);

        Task<QuestionnaireSubmittedResponse> SubmitAsync(string slug, SubmitQuestionnaireDTO request, SubmissionContext context);
    }
}
