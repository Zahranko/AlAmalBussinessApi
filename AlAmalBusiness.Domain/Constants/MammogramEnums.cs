using System;

namespace AlAmalBusiness.Domain.Constants
{
    // The "Female/male" column of the mammogram follow-up sheet.
    public enum Gender
    {
        Female,
        Male
    }

    // The sheet's "Screening/ Diadnostic" column (S / D).
    public enum MammogramExamType
    {
        Screening,
        Diagnostic
    }

    // The sheet's "Procedure" column. The paper sheet writes combinations as
    // "MG+B.U.S+BREAST MRI+BIOPSY", so one visit can be any mix of the four:
    // stored as flags in one int column, sent over the API as a list of names.
    // Stored as int — append new values as the next power of two, never
    // renumber.
    [Flags]
    public enum MammogramProcedures
    {
        None = 0,
        Mammogram = 1,
        BreastUltrasound = 2,
        BreastMri = 4,
        Biopsy = 8
    }

    // Where the WhatsApp follow-up reminder for a row stands. Nothing sends
    // reminders yet — this is the column the integration will read and write,
    // so it can pick up "not sent yet" rows without a schema change. Stored as
    // int — append, never reorder.
    public enum MammogramReminderStatus
    {
        NotSent,
        Sent,
        Failed
    }
}
