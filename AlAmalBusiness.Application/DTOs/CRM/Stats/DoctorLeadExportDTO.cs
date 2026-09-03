using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    // The Hospital Manager's per-doctor Excel export — every lead referred to
    // that doctor (within the selected date range) with its full follow-up
    // history, not just aggregate counts.
    public class DoctorLeadExportDTO
    {
        public string DoctorName { get; set; } = string.Empty;
        public List<DoctorLeadExportRowDTO> Leads { get; set; } = new();
    }

    public class DoctorLeadExportRowDTO
    {
        public string? PatientName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Procedure { get; set; }
        public string? ReferralSource { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public DateTime? CreatedDate { get; set; }

        // One formatted line per follow-up ("[date] actor -> status: note"), oldest first.
        public List<string> FollowUpNotes { get; set; } = new();
    }
}
