using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    // Flat row shape the list queries project into straight from SQL — only
    // the columns the list pages display, with the related names read via
    // the navigation in the same SELECT. Replaces materializing a full Lead
    // plus six Include'd entities (two of them AspNetUsers rows carrying
    // password hashes/security stamps, plus the Description/ClinicSignature
    // LOB columns) per row when all a list needs is a dozen scalars.
    public class LeadListRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public LeadStatus Status { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public string? ReferalName { get; set; }
        public string? ProcedureName { get; set; }
        public string? DoctorName { get; set; }
        public string? ClosedReasonName { get; set; }

        // Most recently added call (by CreatedAt) — only populated by the
        // calendar query (GetAllLeadsAsync); null elsewhere.
        public DateTime? LastCallDate { get; set; }
        public string? LastCallNote { get; set; }
    }
}
