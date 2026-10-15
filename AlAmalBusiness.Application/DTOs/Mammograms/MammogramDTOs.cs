using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Mammograms
{
    // Opens the sheet for one month. One per month; a second open answers 409.
    public class OpenMammogramSheetDTO
    {
        [Range(2000, 2100)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }
    }

    public class MammogramSheetResponse
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        // "9-2026", the tab name the Excel workbook used.
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedByName { get; set; }
        public int RecordCount { get; set; }
        public int FollowUpCount { get; set; }
        public int ContactedCount { get; set; }
    }

    // Create and update body — update is a full replace, like the user DTOs.
    // The reminder fields are not here on purpose: they belong to the
    // WhatsApp integration, not to the person typing the sheet.
    //
    // No data annotations: MammogramService checks every rule (lengths
    // included) and answers in words. The grid sends many of these in one
    // batch, where an annotation failure on one row would 400 the lot.
    public class SaveMammogramRecordDTO
    {
        public string? PatientMrn { get; set; }

        public Gender Gender { get; set; } = Gender.Female;
        public string? FullName { get; set; }
        public string? Tech { get; set; }
        public int? Age { get; set; }

        public bool BreastFeeding { get; set; }
        public bool PossiblePregnancy { get; set; }
        public bool HasImplants { get; set; }
        public DateOnly? VisitDate { get; set; }

        // ["Mammogram", "BreastUltrasound", "BreastMri", "Biopsy"], any mix.
        public List<MammogramProcedures>? Procedures { get; set; }

        public MammogramExamType? ExamType { get; set; }

        // 0-6, optionally 4A/4B/4C, or two results as "0/4".
        public string? BiRads { get; set; }
        public string? Recommendation { get; set; }

        public DateOnly? FollowUpDate { get; set; }

        // Defaults to +962. The number may be typed with or without its
        // leading zero, or with the country code in front.
        public string? PhoneCountryCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactNote { get; set; }

        public DateTime? ContactedAt { get; set; }
    }

    // One row of a grid save: Id for an existing row, none for a new one.
    // ClientKey is the grid's own handle for a row that has no Id yet, echoed
    // back so the result can be matched to the row that sent it.
    public class SaveMammogramRowDTO : SaveMammogramRecordDTO
    {
        public int? Id { get; set; }
        public string? ClientKey { get; set; }
    }

    // Per-row outcome of a grid save: the saved row, or why it wasn't.
    public class MammogramRowResult
    {
        public string? ClientKey { get; set; }
        public int? Id { get; set; }
        public MammogramRecordResponse? Record { get; set; }
        public string? Error { get; set; }
    }

    public class MammogramRecordResponse
    {
        public int Id { get; set; }
        public int SheetId { get; set; }
        public string PatientMrn { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Tech { get; set; }
        public int? Age { get; set; }
        public bool BreastFeeding { get; set; }
        public bool PossiblePregnancy { get; set; }
        public bool HasImplants { get; set; }
        public DateOnly VisitDate { get; set; }
        public List<MammogramProcedures> Procedures { get; set; } = new();
        public MammogramExamType? ExamType { get; set; }
        public string? BiRads { get; set; }
        public string? Recommendation { get; set; }
        public DateOnly? FollowUpDate { get; set; }
        public string PhoneCountryCode { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        // The number as WhatsApp's API wants it: country code + national
        // number, digits only, no "+" ("962799832795"). Null with no phone.
        public string? WhatsAppNumber { get; set; }

        public string? ContactNote { get; set; }
        public DateTime? ContactedAt { get; set; }
        public MammogramReminderStatus ReminderStatus { get; set; }
        public DateTime? ReminderSentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // {Success, NotFound, Conflict, Error, Item}: 404 / 409 / 400 / 200.
    public class MammogramResult<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public bool Conflict { get; set; }
        public string? Error { get; set; }
        public T? Item { get; set; }
    }
}
