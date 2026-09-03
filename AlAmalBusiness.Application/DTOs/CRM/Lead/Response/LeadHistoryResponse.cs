using System;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // One entry on the lead's timeline.
    public class LeadHistoryResponse
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? ResultingStatus { get; set; }
        public string? ActorName { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? DoctorName { get; set; }
        public string? ClosedReasonName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
