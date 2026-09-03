using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    public class DashboardKpiDTO
    {
        public KpiMetricDTO LeadsMonth { get; set; } = new();
        public KpiMetricDTO LeadsToday { get; set; } = new();
        public KpiMetricDTO SuccessMonth { get; set; } = new();
        public KpiMetricDTO SuccessToday { get; set; } = new();
    }

    // Direction/DeltaPercent compare Value against the immediately preceding
    // period of equal length (yesterday for "today", last month for "month").
    // Trend is the last 7 days of daily counts, oldest first.
    public class KpiMetricDTO
    {
        public int Value { get; set; }
        public double DeltaPercent { get; set; }
        public string Direction { get; set; } = "flat";
        public List<int> Trend { get; set; } = new();
    }
}
