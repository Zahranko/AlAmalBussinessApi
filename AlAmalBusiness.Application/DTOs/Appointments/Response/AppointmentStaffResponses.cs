using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Appointments.Response
{
    // One row of the staff inbox.
    public class AppointmentListItemResponse
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Status { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int ReferralSourceId { get; set; }
        public string? ReferralSourceName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // The detail view: the row, what the patient wrote, and the timeline.
    // Details is deliberately absent from the list rows — no list shows it.
    public class AppointmentDetailResponse : AppointmentListItemResponse
    {
        public string? Details { get; set; }
        public List<AppointmentHistoryResponse> History { get; set; } = new();
    }

    // One entry on the request's timeline.
    public class AppointmentHistoryResponse
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? FromStatus { get; set; }
        public string? ToStatus { get; set; }
        // Set on Forwarded entries only.
        public string? FromDepartmentName { get; set; }
        public string? ToDepartmentName { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Result wrapper for the workflow actions that return the full detail
    // (status change, assign, note). NotFound is kept apart from a plain
    // failure so the controller can answer 404 for a request the caller may
    // not see, rather than 409 with a message confirming that the id exists.
    public class AppointmentActionResponse
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public AppointmentDetailResponse? Appointment { get; set; }
    }

    // The appointment dashboard. A manager reads it for their own
    // department; an admin reads the same numbers across every department,
    // with Departments filled in so they can see which one is behind.
    //
    // Every count is over the requests *received* in the period, so the whole
    // screen answers one question rather than mixing arrival and booking
    // windows.
    public class AppointmentStatsResponse
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        // Null for a manager (their own department is the only one they see),
        // set when an admin narrowed to one.
        public int? DepartmentId { get; set; }

        public int Total { get; set; }
        public int NewCount { get; set; }
        public int ContactedCount { get; set; }
        public int ScheduledCount { get; set; }
        public int CancelledCount { get; set; }

        // Share of the period's requests that ended up booked, 0-100.
        public double ScheduledPercent { get; set; }

        // Arrival to booking, in hours. Null when nothing in the period has
        // been booked yet — which is not the same as zero, so it stays
        // nullable all the way to the browser.
        public double? AvgBookingHours { get; set; }

        // The oldest request still sitting in New or Contacted, in hours.
        public double? OldestOpenHours { get; set; }

        public List<AppointmentSourceStatResponse> Sources { get; set; } = new();
        public List<AppointmentBookerStatResponse> Bookers { get; set; } = new();
        public List<AppointmentDepartmentStatResponse> Departments { get; set; } = new();
    }

    public class AppointmentSourceStatResponse
    {
        public int ReferralSourceId { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
        public double Percent { get; set; }
    }

    // Who is booking the requests. Counted per booking rather than per
    // request — see AppointmentBookerRow for why.
    public class AppointmentBookerStatResponse
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public int ScheduledCount { get; set; }
        public double? AvgBookingHours { get; set; }
    }

    public class AppointmentDepartmentStatResponse
    {
        public int DepartmentId { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int ScheduledCount { get; set; }
        public int CancelledCount { get; set; }
        public double ScheduledPercent { get; set; }
        public double? AvgBookingHours { get; set; }
    }
}
