using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Questionnaires.Response
{
    // ---------- list screen ----------

    public class QuestionnaireListResponse
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public int QuestionnaireCount { get; set; }
        public int ActiveCount { get; set; }
        public int TotalSubmissions { get; set; }

        // Weighted over every answer in scope, not an average of averages —
        // a questionnaire with 400 responses outweighs one with 3.
        public double? AverageRating { get; set; }

        // Share of answers rated Good or VeryGood.
        public double? SatisfactionPercent { get; set; }

        public List<QuestionnaireSummaryResponse> Questionnaires { get; set; } = new();
    }

    public class QuestionnaireSummaryResponse
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
        public double? AverageRating { get; set; }
        public double? SatisfactionPercent { get; set; }
        public DateTime? LastSubmissionDate { get; set; }
    }

    // ---------- edit form ----------

    public class QuestionnaireDetailResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        // Live questions only, in page order.
        public List<QuestionResponse> Questions { get; set; } = new();
    }

    public class QuestionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class QuestionnaireActionResponse
    {
        public bool Success { get; set; }
        // Kept apart from a plain failure so the controller answers 404 for a
        // questionnaire outside the caller's department, not 409.
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public QuestionnaireDetailResponse? Questionnaire { get; set; }
    }

    // ---------- results screen ----------

    public class QuestionnaireStatsResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public int SubmissionCount { get; set; }
        public double? AverageRating { get; set; }
        public double? SatisfactionPercent { get; set; }

        // Every rating across every question, for the overall distribution bar.
        public RatingDistributionResponse Distribution { get; set; } = new();

        public List<QuestionStatsResponse> Questions { get; set; } = new();
    }

    public class QuestionStatsResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        // Removed from the page after it had answers — listed last, and only
        // when it has answers in the period.
        public bool IsArchived { get; set; }
        public int AnswerCount { get; set; }
        public double? AverageRating { get; set; }
        public double? SatisfactionPercent { get; set; }
        public RatingDistributionResponse Distribution { get; set; } = new();
    }

    public class RatingDistributionResponse
    {
        public int VeryGood { get; set; }
        public int Good { get; set; }
        public int Mid { get; set; }
        public int Bad { get; set; }
        public int VeryBad { get; set; }
    }

    public class QuestionnaireSubmissionResponse
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public double? AverageRating { get; set; }
    }

    // ---------- export ----------

    // Everything the per-questionnaire workbook needs, already scoped: the
    // results screen's numbers plus each response with its ratings.
    public class QuestionnaireExportData
    {
        public QuestionnaireStatsResponse Stats { get; set; } = new();
        public List<QuestionnaireExportResponseRow> Responses { get; set; } = new();
        // True when the period held more responses than the export cap.
        public bool Truncated { get; set; }
        public int ExportCap { get; set; }
    }

    public class QuestionnaireExportResponseRow
    {
        public DateTime CreatedDate { get; set; }
        public string? Name { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Notes { get; set; }
        public double? AverageRating { get; set; }
        public Dictionary<int, AlAmalBusiness.Domain.Constants.QuestionRating> Ratings { get; set; } = new();
    }

    // ---------- public page ----------

    // Everything the anonymous page needs to render, and nothing else — no
    // ids of other questionnaires, no counts, no department id.
    public class PublicQuestionnaireResponse
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DepartmentName { get; set; }
        public List<PublicQuestionResponse> Questions { get; set; } = new();
    }

    public class PublicQuestionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class QuestionnaireSubmittedResponse
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
    }
}
