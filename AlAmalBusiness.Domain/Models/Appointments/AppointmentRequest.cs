using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // A request a patient sent through the public (anonymous) appointment
    // page. Staff are told about it by email (AppointmentService) — there is
    // no workflow on the row itself yet.
    //
    // ProcedureId/ReferralSourceId point at the appointment site's OWN lookup
    // lists, not the CRM's Procedures/Referals: the two are maintained by
    // different people for different audiences, and retiring a CRM entry must
    // not silently change what patients can pick here.
    public class AppointmentRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        // Stored like PatientFeedback: country code in its own column, the
        // national number digits-only with its leading zero stripped.
        public string PhoneCountryCode { get; set; } = "+962";
        public string PhoneNumber { get; set; } = string.Empty;

        public int ProcedureId { get; set; }
        public AppointmentProcedure? Procedure { get; set; }

        public int ReferralSourceId { get; set; }
        public AppointmentReferralSource? ReferralSource { get; set; }

        public string? Details { get; set; }

        // Audit — captured server-side, never sent by the browser.
        public string? SubmittedFromIp { get; set; }
        public string? UserAgent { get; set; }

        // Jordan time, like every business timestamp (AppClock).
        public DateTime CreatedDate { get; set; } = AppClock.Now;
    }
}
