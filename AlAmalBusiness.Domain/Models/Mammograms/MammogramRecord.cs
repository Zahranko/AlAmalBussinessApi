using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Mammograms
{
    // One row of a monthly mammogram sheet: one patient visit, what was done,
    // the BI-RADS result, what the radiologist recommended and when the
    // patient is due back. Column-for-column the Excel sheet's 9-2026 layout.
    //
    // The phone and follow-up date are the two fields a WhatsApp reminder
    // needs, so they are stored ready for it: the phone as country code +
    // national digits (leading zero stripped, the feedback/appointment
    // convention, which also matches what Excel did to the old numbers), and
    // the follow-up as a plain date with an index. ReminderStatus/SentAt/
    // MessageId are reserved for that integration and nothing writes them yet.
    public class MammogramRecord
    {
        [Key]
        public int Id { get; set; }

        public int SheetId { get; set; }
        public MammogramSheet? Sheet { get; set; }

        // "ID" — the hospital file number (MRN). Text, not int: it is an
        // identifier, never arithmetic.
        [Required]
        public string PatientMrn { get; set; } = string.Empty;

        public Gender Gender { get; set; } = Gender.Female;

        [Required]
        public string FullName { get; set; } = string.Empty;

        // "Tech" — who performed it (EMAN, DR.DOAA ...). Free text; the
        // console autocompletes from the names already used.
        public string? Tech { get; set; }

        public int? Age { get; set; }

        public bool BreastFeeding { get; set; }
        public bool PossiblePregnancy { get; set; }
        public bool HasImplants { get; set; }

        public DateOnly VisitDate { get; set; }

        public MammogramProcedures Procedures { get; set; }

        public MammogramExamType? ExamType { get; set; }

        // "B" — BI-RADS category, 0-6, optionally 4A/4B/4C, and the sheet's
        // "0/4" form for two results (e.g. initial/final). Kept as text.
        public string? BiRads { get; set; }

        // "Pros" — the radiologist's recommendation.
        public string? Recommendation { get; set; }

        // "Follow Up-Date" — when the patient is due back. Null when the
        // recommendation is not a timed follow-up (biopsy, MRI ...).
        public DateOnly? FollowUpDate { get; set; }

        // "Call Follow Up".
        public string PhoneCountryCode { get; set; } = "+962";
        public string? PhoneNumber { get; set; }

        // "التواصل" / "وقت التواصل" — the outcome of the follow-up call, and
        // when it happened.
        public string? ContactNote { get; set; }
        public DateTime? ContactedAt { get; set; }

        // Reserved for the WhatsApp integration (see the class comment).
        // Reset to NotSent whenever the phone or the follow-up date changes,
        // so a corrected row is reminded again.
        public MammogramReminderStatus ReminderStatus { get; set; } = MammogramReminderStatus.NotSent;
        public DateTime? ReminderSentAt { get; set; }
        public string? ReminderMessageId { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = AppClock.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
