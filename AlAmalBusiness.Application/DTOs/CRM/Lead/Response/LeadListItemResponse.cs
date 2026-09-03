using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // Row shape for the lead list pages.
    public class LeadListItemResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public LeadStatus Status { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ReferalName { get; set; }
        public string? ProcedureName { get; set; }
        public string? DoctorName { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public string? ClosedReason { get; set; }

        // The most recently logged call for this lead (by CreatedAt, i.e. the
        // last one added — not necessarily the soonest-scheduled one). Null
        // for a lead with no calls yet. Only populated by GetAllLeadsAsync
        // today (see LeadService.ToListItem) — the case calendar is the only
        // consumer so far.
        public DateTime? LastCallDate { get; set; }
        public string? LastCallNote { get; set; }
    }
}
