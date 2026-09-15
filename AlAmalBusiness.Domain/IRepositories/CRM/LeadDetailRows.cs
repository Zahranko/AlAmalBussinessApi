using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    // The lead detail screen, projected straight from SQL like LeadListRow:
    // names come through the navigations in the same SELECT, so no
    // AspNetUsers row (password hash, security stamp) is ever materialized to
    // read a username.
    public class LeadDetailRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CountryKey { get; set; }
        public string? PhoneNum { get; set; }
        public string? NickName { get; set; }
        public string? Description { get; set; }
        public LeadStatus Status { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public bool HasDoctor { get; set; }
        public int? DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? ClinicSignature { get; set; }
        public int ReferalId { get; set; }
        public string? ReferalName { get; set; }
        public int ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ClosedReasonName { get; set; }
    }

    public class LeadHistoryRow
    {
        public int Id { get; set; }
        public LeadActions Type { get; set; }
        public LeadStatus? ResultingStatus { get; set; }
        public string? ActorName { get; set; }
        public DateTime? ActionDate { get; set; }
        public string? DoctorName { get; set; }
        public string? ClosedReasonName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeadCallRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public bool IsDone { get; set; }
        public string? ActorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // The per-doctor export: one row per lead, no Description/ClinicSignature.
    public class DoctorLeadRow
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public LeadStatus Status { get; set; }
        public string? ProcedureName { get; set; }
        public string? ReferalName { get; set; }
        public string? CreatedByName { get; set; }
        public string? ClaimedByName { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    // One follow-up line of that export.
    public class LeadFollowUpRow
    {
        public int LeadId { get; set; }
        public DateTime? ActionDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ActorName { get; set; }
        public LeadStatus? ResultingStatus { get; set; }
        public string? Note { get; set; }
    }
}
