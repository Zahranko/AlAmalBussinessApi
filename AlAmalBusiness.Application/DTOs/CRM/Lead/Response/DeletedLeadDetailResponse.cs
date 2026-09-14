using System;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // A deleted lead as the admin sees it before deciding to restore: the
    // full lead (timeline and calls included) plus who deleted it, when, why.
    public class DeletedLeadDetailResponse
    {
        public LeadDetailResponse Lead { get; set; } = new();
        public string? DeletedById { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? Reason { get; set; }
    }
}
