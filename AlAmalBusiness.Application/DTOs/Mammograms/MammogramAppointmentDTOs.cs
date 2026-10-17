using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Mammograms
{
    // Books a slot on the day calendar. MammogramAppointmentService checks
    // every rule and answers in words, so there are no annotations here — the
    // same choice as SaveMammogramRecordDTO.
    public class BookMammogramAppointmentDTO
    {
        public string? FullName { get; set; }

        // Defaults to +962; the number may be typed with or without its
        // leading zero, or with the country code in front.
        public string? PhoneCountryCode { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Notes { get; set; }

        // Jordan local time, on a quarter hour between 08:00 and 16:45.
        public DateTime? AppointmentAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Room { get; set; } = 1;

        public List<MammogramProcedures>? Procedures { get; set; }
        public string? ReferringDoctor { get; set; }
        public string? ReceivingDoctor { get; set; }
    }

    // The new slot when a follow-up moves the appointment.
    public class MammogramAppointmentSlotDTO
    {
        public DateTime? AppointmentAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Room { get; set; }
    }

    // Status is FollowUp (a call; may carry a new slot and whether the
    // patient was reached), Success, or Failed (needs a note).
    public class MammogramAppointmentFollowUpDTO
    {
        public MammogramAppointmentStatus Status { get; set; }
        public string? Note { get; set; }
        public bool? CallSucceeded { get; set; }
        public MammogramAppointmentSlotDTO? Reschedule { get; set; }
    }

    public class MammogramAppointmentResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneCountryCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        // "962799832795" — what WhatsApp and CertaCure take.
        public string? WhatsAppNumber { get; set; }
        public string? Notes { get; set; }
        public MammogramAppointmentStatus Status { get; set; }
        public DateTime AppointmentAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Room { get; set; }
        public List<MammogramProcedures> Procedures { get; set; } = new();
        public string? ReferringDoctor { get; set; }
        public string? ReceivingDoctor { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class MammogramAppointmentHistoryResponse
    {
        public int Id { get; set; }
        public MammogramAppointmentActions Action { get; set; }
        public MammogramAppointmentStatus? ResultingStatus { get; set; }
        public string? ActorName { get; set; }
        public DateTime? AppointmentAt { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Room { get; set; }
        public bool? CallSucceeded { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MammogramAppointmentDetailResponse : MammogramAppointmentResponse
    {
        public List<MammogramAppointmentHistoryResponse> History { get; set; } = new();
    }

    // One month of bookings (by the day they were booked, as the old
    // dashboard counted), plus who is in the building right now.
    public class MammogramAppointmentStatsResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Total { get; set; }
        public int Booked { get; set; }
        public int FollowUp { get; set; }
        public int Arrived { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        public int ArrivedNow { get; set; }
        // Each procedure counted on every appointment that included it, so
        // the rows can add up to more than Total. Most used first.
        public List<MammogramProcedureCount> Procedures { get; set; } = new();
    }

    public class MammogramProcedureCount
    {
        public MammogramProcedures Procedure { get; set; }
        public int Count { get; set; }
    }

    // The printed monthly report: the same month meaning as the stats card
    // and the Excel export (by the day each appointment was booked), so the
    // three never disagree, broken down for paper and followed by every
    // appointment in the month.
    public class MammogramAppointmentReportResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Total { get; set; }
        public int Booked { get; set; }
        public int FollowUp { get; set; }
        public int Arrived { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
        // Success out of the appointments that reached an outcome; null when
        // none has yet, never 0.
        public double? SuccessRate { get; set; }
        public List<MammogramProcedureCount> Procedures { get; set; } = new();
        public List<MammogramReportGroup> Rooms { get; set; } = new();
        public List<MammogramReportGroup> BookedBy { get; set; } = new();
        public List<MammogramReportGroup> ReferringDoctors { get; set; } = new();
        public List<MammogramAppointmentResponse> Items { get; set; } = new();
    }

    // One row of a breakdown: how many of the month's appointments fell
    // under it and how they ended.
    public class MammogramReportGroup
    {
        public string Name { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Success { get; set; }
        public int Failed { get; set; }
    }

    public class MammogramAppointmentListQueryDTO
    {
        public MammogramAppointmentView View { get; set; } = MammogramAppointmentView.Today;
        public MammogramAppointmentStatus? Status { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
