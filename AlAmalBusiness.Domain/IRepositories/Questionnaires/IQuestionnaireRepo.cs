using AlAmalBusiness.Domain.Models.Questionnaires;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Questionnaires
{
    public interface IQuestionnaireRepo
    {
        // The list screen: every questionnaire in scope with its response
        // count and average, aggregated in SQL. restrictToDepartmentIds is the
        // caller's scoping (set by the service, never by the request): null is
        // unrestricted (Admin), otherwise every department they may read, and
        // an empty list is "nothing". The dates bound which submissions are
        // counted, not which questionnaires are listed.
        Task<List<QuestionnaireSummaryRow>> GetSummariesAsync(List<int>? restrictToDepartmentIds, DateOnly? from, DateOnly? to);

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

        // Per-month numbers for these questionnaires, for submissions in
        // [from, toExclusive). Two grouped queries, no answer rows. Months with
        // nothing in them are simply absent.
        Task<List<QuestionnaireMonthRow>> GetMonthlyAsync(IReadOnlyCollection<int> questionnaireIds, DateTime from, DateTime toExclusive);
        // Every question (archived included) of these questionnaires, in page order.
        Task<List<QuestionnaireQuestionRow>> GetQuestionsAsync(IReadOnlyCollection<int> questionnaireIds);
        // Per-question, per-rating counts for these questionnaires over every
        // submission before periodEndExclusive, flagged InPeriod when on or
        // after periodStart. One grouped query, no answer rows.
        Task<List<QuestionnairePeriodRatingRow>> GetRatingCountsSplitAsync(
            IReadOnlyCollection<int> questionnaireIds, DateTime periodStart, DateTime periodEndExclusive);

        // Claims a month for the monthly report. False when it was already
        // claimed (the unique (Year, Month) index refused the insert).
        Task<QuestionnaireReportRun?> TryClaimReportRunAsync(int year, int month);

        // The scheduler's cheap "already done?" check before it builds anything.
        Task<bool> HasReportRunAsync(int year, int month);

        void Add(Questionnaire questionnaire);
        void Remove(Questionnaire questionnaire);
        void RemoveQuestion(QuestionnaireQuestion question);
        void AddSubmission(QuestionnaireSubmission submission);

        Task SaveChangesAsync();
    }
}
