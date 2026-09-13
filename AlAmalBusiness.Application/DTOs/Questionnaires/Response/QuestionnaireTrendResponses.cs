using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Questionnaires.Response
{
    // One calendar month on a trend chart. Averages/percents are null (not 0)
    // for a month nobody answered — the chart leaves a gap there.
    public class MonthPointResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        // "2026-09" — sortable, and reads the same in both languages.
        public string Label { get; set; } = string.Empty;
        public int Submissions { get; set; }
        public double? AverageRating { get; set; }
        public double? SatisfactionPercent { get; set; }
    }

    // A block of time summed into one line: "this month", "all previous months".
    public class PeriodSummaryResponse
    {
        public string Label { get; set; } = string.Empty;
        public int Submissions { get; set; }
        public double? AverageRating { get; set; }
        public double? SatisfactionPercent { get; set; }
    }

    public class QuestionnaireTrend
    {
        // Oldest first, contiguous (empty months included), ending at the report month.
        public List<MonthPointResponse> Months { get; set; } = new();
        public PeriodSummaryResponse ReportMonth { get; set; } = new();
        public PeriodSummaryResponse PreviousMonths { get; set; } = new();
    }
}
