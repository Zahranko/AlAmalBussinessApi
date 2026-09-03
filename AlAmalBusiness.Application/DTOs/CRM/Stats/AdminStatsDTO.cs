using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    public class AdminStatsDTO
    {
        public int TotalLeads { get; set; }
        public int SuccessCount { get; set; }
        public int ClosedCount { get; set; }
        public double SuccessPercent { get; set; }
        public double ClosedPercent { get; set; }
        public List<ReferralSourceStatDTO> ReferralSources { get; set; } = new();
        public List<EmployeeStatDTO> Employees { get; set; } = new();
    }

    public class ReferralSourceStatDTO
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    public class EmployeeStatDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int TotalCreated { get; set; }
        public int SuccessCount { get; set; }
        public int ClosedCount { get; set; }
        public double Percent { get; set; }
    }
}
