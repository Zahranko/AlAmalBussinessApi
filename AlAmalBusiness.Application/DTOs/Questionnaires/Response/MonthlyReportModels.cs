using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Questionnaires.Response
{
    // What one department's monthly questionnaire email is built from.
    public class DepartmentMonthlyReport
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        // Every questionnaire of the department folded together.
        public QuestionnaireTrend Totals { get; set; } = new();
        public List<QuestionnaireMonthlyReport> Questionnaires { get; set; } = new();
    }

    public class QuestionnaireMonthlyReport
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public QuestionnaireTrend Trend { get; set; } = new();
        public List<QuestionCompareRow> Questions { get; set; } = new();
    }

    // One question: the report month against every month before it.
    public class QuestionCompareRow
    {
        public string Text { get; set; } = string.Empty;
        public int AnswersThisMonth { get; set; }
        public double? AverageThisMonth { get; set; }
        public double? AverageBefore { get; set; }
        public double? SatisfiedThisMonth { get; set; }
        public double? SatisfiedBefore { get; set; }
    }

    public class MonthlyReportSendResult
    {
        public int Year { get; set; }
        public int Month { get; set; }
        // The scheduled run found the month already sent (or email is off) and did nothing.
        public bool Skipped { get; set; }
        public string? SkipReason { get; set; }
        public int EmailsQueued { get; set; }
        public List<MonthlyReportDepartmentResult> Departments { get; set; } = new();
    }

    public class MonthlyReportDepartmentResult
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Questionnaires { get; set; }
        public List<string> Recipients { get; set; } = new();
        public int EmailsQueued { get; set; }
    }
}
