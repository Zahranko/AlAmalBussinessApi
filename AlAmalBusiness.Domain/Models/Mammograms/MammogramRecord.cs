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
    // the follow-up as a plain date with an index. The Reminder* columns are
    // written by MammogramReminderService (2026-10-17): the radiologyreminde
    // template goes out through the WhatsApp platform on the follow-up day.
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

        // "نوع الفحص" (added 2026-10-17) — the exam the patient is due back
        // for, in Arabic, exactly as the WhatsApp reminder should say it: it is
        // the template's {{1}}. Blank falls back to reading the recommendation
        // (MammogramReminderService.ExamText).
        [MaxLength(100)]
        public string? FollowUpExam { get; set; }

        // "Follow Up-Date 2" / "نوع الفحص 2" (added 2026-10-19) — a second
        // return visit, after the first, with its own exam and its own
        // WhatsApp reminder (the Reminder2* columns below). Console-only: the
        // Excel export keeps the old workbook's columns and leaves both out.
        public DateOnly? FollowUpDate2 { get; set; }
        [MaxLength(100)]
        public string? FollowUpExam2 { get; set; }

        // "Call Follow Up".
        public string PhoneCountryCode { get; set; } = "+962";
        public string? PhoneNumber { get; set; }

        // "التواصل" / "وقت التواصل" — the outcome of the follow-up call, and
        // when it happened.
        public string? ContactNote { get; set; }
        public DateTime? ContactedAt { get; set; }

        // The WhatsApp reminder (see the class comment). Reset to NotSent
        // whenever the phone or the follow-up date changes, so a corrected
        // row is reminded again. MessageId is the WhatsApp platform's own
        // message id, which its status is read back by.
        public MammogramReminderStatus ReminderStatus { get; set; } = MammogramReminderStatus.NotSent;
        public DateTime? ReminderSentAt { get; set; }
        public string? ReminderMessageId { get; set; }
        [MaxLength(500)]
        public string? ReminderError { get; set; }

        // The second follow-up's reminder, the same four fields. Reset when
        // the phone or FollowUpDate2 changes.
        public MammogramReminderStatus Reminder2Status { get; set; } = MammogramReminderStatus.NotSent;
        public DateTime? Reminder2SentAt { get; set; }
        public string? Reminder2MessageId { get; set; }
        [MaxLength(500)]
        public string? Reminder2Error { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = AppClock.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
