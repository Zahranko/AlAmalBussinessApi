using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead
{
    // A follow-up sets the lead's status to one of the workflow outcomes.
    // Waiting: optional appointment date/doctor.
    // Success: optional clinic doctor assignment + signature.
    // Pending: requires Notes; optional contact-info corrections (null = don't touch).
    // Closed: requires ClosedReasonId.
    public class FollowUpLeadDTO
    {
        [Required]
        public LeadStatus Status { get; set; }

        public DateTime? Date { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public bool? HasDoctor { get; set; }
        public int? DoctorId { get; set; }
        public int? ClosedReasonId { get; set; }

        // An image data URL (checked in LeadService). Capped because it lands
        // in a LOB column and comes back on every detail response: ~3M
        // characters is a ~2 MB image, far above a scanned signature.
        [StringLength(3_000_000, ErrorMessage = "The signature image is too large (about 2 MB at most).")]
        public string? SignatureData { get; set; }

        // Optional contact-info corrections — only applied when Status == Pending.
        // Null means "don't touch" (omitted entirely by a caller unaware of this).
        [StringLength(200)]
        public string? Name { get; set; }
        [StringLength(10)]
        public string? CountryKey { get; set; }
        [StringLength(32)]
        public string? PhoneNum { get; set; }
        [StringLength(100)]
        public string? NickName { get; set; }

        // Optional payment-way correction, applied when Status == Waiting or Pending.
        public PaymentWays? PaymentWay { get; set; }
    }
}
