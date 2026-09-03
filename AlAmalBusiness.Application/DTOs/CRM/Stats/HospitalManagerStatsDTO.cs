using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    public class HospitalManagerStatsDTO
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int TotalLeads { get; set; }
        public int PendingCount { get; set; }
        public int WaitingCount { get; set; }
        public int SuccessCount { get; set; }
        public int ClosedCount { get; set; }
        public double PendingPercent { get; set; }
        public double WaitingPercent { get; set; }
        public double SuccessPercent { get; set; }
        public double ClosedPercent { get; set; }
        public List<GroupStatDTO> Doctors { get; set; } = new();
        public List<GroupStatDTO> Procedures { get; set; } = new();
    }

    // One doctor's or one procedure's lead counts + success rate.
    public class GroupStatDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int TotalLeads { get; set; }
        public int PendingCount { get; set; }
        public int WaitingCount { get; set; }
        public int SuccessCount { get; set; }
        public int ClosedCount { get; set; }
        public double SuccessRate { get; set; }
        public List<DoctorProcedureStatDTO> Procedures { get; set; } = new();
    }

    public class DoctorProcedureStatDTO
    {
        public int ProcedureId { get; set; }
        public string ProcedureName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percent { get; set; }
    }
}
