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

    public class QuestionRatingCountRow
    {
        public int QuestionId { get; set; }
        public QuestionRating Rating { get; set; }
        public int Count { get; set; }
    }
}
