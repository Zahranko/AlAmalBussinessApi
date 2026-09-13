using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Questionnaires
{
    // Projection the list screen reads through — no question text, no answer
    // rows, just the numbers.
    public class QuestionnaireSummaryRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public int QuestionCount { get; set; }
        public int SubmissionCount { get; set; }
        // Over every answer in the period; null when nothing was answered.
        public double? AverageRating { get; set; }
        // Answers rated Good or VeryGood, and every answer — the satisfaction share.
        public int PositiveAnswers { get; set; }
        public int TotalAnswers { get; set; }
        public DateTime? LastSubmissionDate { get; set; }
    }

    // One response in the results screen's list — the patient's optional
    // contact details and their own average, not the individual answers.
    public class QuestionnaireSubmissionRow
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public double? AverageRating { get; set; }
    }

    // One questionnaire's numbers for one calendar month. RatingSum/AnswerCount
    // give the average, Positive/AnswerCount the satisfied share.
    public class QuestionnaireMonthRow
    {
        public int QuestionnaireId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Submissions { get; set; }
        public int AnswerCount { get; set; }
        public int RatingSum { get; set; }
        public int Positive { get; set; }
    }

    // One rating inside one response, for the export's per-response sheet.
    public class QuestionnaireAnswerExportRow
    {
        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public QuestionRating Rating { get; set; }
    }

    public class QuestionRatingCountRow
    {
        public int QuestionId { get; set; }
        public QuestionRating Rating { get; set; }
        public int Count { get; set; }
    }
}
