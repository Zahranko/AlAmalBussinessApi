using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // A request a patient sent through the public (anonymous) appointment
    // page, and the workflow the booking team runs it through afterwards —
    // the same shape as PatientFeedback, for the same reasons.
    //
    // DepartmentId points at the shared Departments lookup, the table
    // User.DepartmentId already points at: that is what makes "staff see
    // their own department's requests" a plain column comparison instead of
    // a second parallel department list. It replaced an appointment-only
    // Procedures list (2026-09-16) — a procedure told nobody who owned the
    // request, and the notification had to be routed by an
    // admin-maintained address list because of it.
    //
    // ReferralSourceId still points at the appointment site's OWN list, not
    // the CRM's Referals: the two are maintained by different people for
    // different audiences, and retiring a CRM entry must not silently change
    // what patients can pick here.
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

        [Required]
        public int DepartmentId { get; set; }
        public Departments? Department { get; set; }

        public int ReferralSourceId { get; set; }
        public AppointmentReferralSource? ReferralSource { get; set; }

        public string? Details { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.New;

        // The staff member who took it on. A real FK to AspNetUsers, read
        // back through the navigation — no username-snapshot column, same as
        // PatientFeedback.AssignedToId.
        public string? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        // When the request became a booked appointment (Status = Scheduled).
        // Cleared again if it ever leaves that status, so "time to book"
        // never averages over a stamp that no longer describes the row.
        public DateTime? CompletedAt { get; set; }

        // Audit — captured server-side, never sent by the browser.
        public string? SubmittedFromIp { get; set; }
        public string? UserAgent { get; set; }

        // Jordan time, like every business timestamp (AppClock).
        public DateTime CreatedDate { get; set; } = AppClock.Now;
    }
}
