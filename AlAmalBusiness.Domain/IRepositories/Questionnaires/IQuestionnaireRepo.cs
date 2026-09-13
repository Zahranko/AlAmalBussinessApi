using AlAmalBusiness.Domain.Models.Questionnaires;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Questionnaires
{
    public interface IQuestionnaireRepo
    {
        // The list screen: every questionnaire in scope with its response
        // count and average, aggregated in SQL. RestrictToDepartmentId is the
        // caller's scoping (set by the service, never by the request); the
        // dates bound which submissions are counted, not which questionnaires
        // are listed.
        Task<List<QuestionnaireSummaryRow>> GetSummariesAsync(int? restrictToDepartmentId, DateOnly? from, DateOnly? to);

        // Tracked, with every question (archived included), for editing.
        Task<Questionnaire?> GetForEditAsync(int id);

        // Untracked, with department name and live questions, for display.
        Task<Questionnaire?> GetDetailAsync(int id);

        // The public page: active questionnaires only, live questions only.
        Task<Questionnaire?> GetActiveBySlugAsync(string slug);

        Task<bool> IsSlugExist(string slug, int excludeId);

        // Which of these questions have at least one answer — those get
        // archived on removal rather than deleted.
        Task<HashSet<int>> GetAnsweredQuestionIdsAsync(IEnumerable<int> questionIds);

        Task<bool> HasSubmissionsAsync(int questionnaireId);

        // Per-question, per-rating counts for one questionnaire over a period.
        Task<List<QuestionRatingCountRow>> GetRatingCountsAsync(int questionnaireId, DateOnly? from, DateOnly? to);

        Task<int> CountSubmissionsAsync(int questionnaireId, DateOnly? from, DateOnly? to);

        // Newest first. contactOnly narrows to responses that left a name or phone.
        Task<(List<QuestionnaireSubmissionRow> Items, int TotalCount)> PageSubmissionsAsync(
            int questionnaireId, DateOnly? from, DateOnly? to, bool contactOnly, int page, int pageSize);

        // The export's per-response sheet: newest responses in the period (at
        // most `max`), and every rating those responses gave. Two flat queries.
        Task<(List<QuestionnaireSubmissionRow> Submissions, List<QuestionnaireAnswerExportRow> Answers)> GetExportRowsAsync(
            int questionnaireId, DateOnly? from, DateOnly? to, int max);

        void Add(Questionnaire questionnaire);
        void Remove(Questionnaire questionnaire);
        void RemoveQuestion(QuestionnaireQuestion question);
        void AddSubmission(QuestionnaireSubmission submission);

        Task SaveChangesAsync();
    }
}
