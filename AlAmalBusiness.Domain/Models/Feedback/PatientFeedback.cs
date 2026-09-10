using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Feedback
{
    // A message a patient submitted through the public (anonymous) form.
    //
    // Named PatientFeedback rather than Feedback so the type doesn't collide
    // with the AlAmalBusiness.Domain.Models.Feedback namespace it lives in.
    //
    // DepartmentId points at the shared Departments lookup — the same table
    // User.DepartmentId points at, which is what makes "staff see their own
    // department's messages" a plain column comparison rather than a second
    // parallel department list to keep in sync.
    public class PatientFeedback
    {
        [Key]
        public int Id { get; set; }

        // Shown to the patient on the success screen, e.g. AMH-K7P2QX. Unique.
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;

        public FeedbackType Type { get; set; }
        public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        // The country code is kept apart from the national number so the two
        // can be formatted independently; the number is stored digits-only
        // with the national leading zero already stripped (see FeedbackService).
        public string? PhoneCountryCode { get; set; } = "+962";
        public string? PhoneNumber { get; set; }

        // The visit the patient is talking about — a date, never a timestamp.
        public DateOnly VisitDate { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public Departments? Department { get; set; }

        public string? Details { get; set; }

        // The staff member who took it on. A real FK to AspNetUsers, read back
        // through the navigation (feedback.AssignedTo.UserName) — the same
        // convention Lead.ClaimedById follows, no username-snapshot column.
        public string? AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        public DateTime? ResolvedAt { get; set; }

        // Audit — captured server-side, never sent by the browser.
        public string? SubmittedFromIp { get; set; }
        public string? UserAgent { get; set; }

        // Local time, matching Lead.CreatedDate — the date filters below
        // compare against local day boundaries accordingly.
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
