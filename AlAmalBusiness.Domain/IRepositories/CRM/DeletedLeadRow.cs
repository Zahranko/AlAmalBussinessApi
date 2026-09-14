using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    // Flat projection for the admin "Deleted leads" list — same reasoning as
    // LeadListRow: no Includes, no LOB columns, names read in the same SELECT.
    public class DeletedLeadRow
    {
        public int LeadId { get; set; }
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public LeadStatus Status { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public string? ProcedureName { get; set; }
        public string? ReferalName { get; set; }

        public string? DeletedById { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime DeletedAt { get; set; }
        public string? Reason { get; set; }
    }
}
