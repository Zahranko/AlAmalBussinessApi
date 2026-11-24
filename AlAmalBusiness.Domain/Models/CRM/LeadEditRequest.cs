using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.CRM
{
    // An employee's proposed correction to a lead (added 2026-11-22 for the
    // mobile app's "Request edit"). Only an Admin edits a lead directly
    // (LeadService.AdminUpdateLeadAsync); everyone else proposes the full set
    // of base fields here and an Admin approves or declines it. The proposed
    // values are stored whole, not as a diff: the diff an admin reviews is
    // computed against the lead as it is *now*, so a lead edited in between
    // shows what approving would actually change.
    public class LeadEditRequest
    {
        [Key]
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead? Lead { get; set; }

        [Required]
        public string? RequestedById { get; set; }
        public User? RequestedBy { get; set; }
        public DateTime RequestedAt { get; set; } = AppClock.Now;

        // Why the employee wants it changed (optional, shown to the admin).
        [MaxLength(500)]
        public string? Note { get; set; }

        public LeadEditRequestStatus Status { get; set; } = LeadEditRequestStatus.Pending;

        public string? ReviewedById { get; set; }
        public User? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        [MaxLength(500)]
        public string? ReviewNote { get; set; }

        // ---- proposed values (same lengths as the Leads columns) ----
        [MaxLength(200)]
        public string? Name { get; set; }
        [MaxLength(10)]
        public string? CountryKey { get; set; }
        [MaxLength(32)]
        public string? PhoneNum { get; set; }
        [MaxLength(100)]
        public string? NickName { get; set; }
        public string? Description { get; set; }
        public PaymentWays? PaymentWay { get; set; }
        public int? DoctorId { get; set; }
        public Doctors? Doctor { get; set; }
        public int ReferalId { get; set; }
        public ReferalSource? Referal { get; set; }
        public int ProcedureId { get; set; }
        public Procedures? Procedure { get; set; }
    }
}
