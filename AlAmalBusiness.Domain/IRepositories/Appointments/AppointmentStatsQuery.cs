using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Domain.IRepositories.Appointments
{
    // What the manager/admin dashboard asks for, and the flat rows it reads
    // back. Everything here is aggregated in SQL — the dashboard must never
    // become "page through every request and count them in C#". Mirrors
    // FeedbackStatsQuery.
    public class AppointmentStatsQuery
    {
        // Inclusive bounds on the day the request arrived, so the whole
        // screen reads as "of the requests received in this period…".
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        // An admin narrowing to one department. Nobody else can send it.
        public int? DepartmentId { get; set; }

        // Set by the service from the caller's roles, never by the caller.
        // Null means "no restriction", which is Admin only.
        public int? RestrictToDepartmentId { get; set; }
    }

    public class AppointmentStatusCountRow
    {
        public Constants.AppointmentStatus Status { get; set; }
        public int Count { get; set; }
    }

    // Where the patients heard about us, over the same period.
    public class AppointmentSourceRow
    {
        public int ReferralSourceId { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    // One member of staff and the requests they booked.
    //
    // Counted per booking, not per request: a request that was scheduled,
    // cancelled and scheduled again is two pieces of work, and if two people
    // did them both are credited. That also keeps this a single grouped
    // SELECT over the timeline rather than a per-request "who touched it
    // last" lookup.
    public class AppointmentBookerRow
    {
        public string? ActorId { get; set; }
        public string? ActorName { get; set; }
        public int ScheduledCount { get; set; }
        // Minutes from the request arriving to that booking.
        public double? AvgMinutes { get; set; }
    }

    public class AppointmentDepartmentRow
    {
        public int DepartmentId { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int NewCount { get; set; }
        public int ContactedCount { get; set; }
        public int ScheduledCount { get; set; }
        public int CancelledCount { get; set; }
        public double? AvgMinutes { get; set; }
    }

    // Everything one dashboard load needs, read in a handful of aggregate
    // queries and assembled by the service.
    public class AppointmentStatsRows
    {
        public List<AppointmentStatusCountRow> Statuses { get; set; } = new();
        public List<AppointmentSourceRow> Sources { get; set; } = new();
        public List<AppointmentBookerRow> Bookers { get; set; } = new();
        public List<AppointmentDepartmentRow> Departments { get; set; } = new();

        // Across every booked request in range, arrival to CompletedAt.
        public double? AvgBookingMinutes { get; set; }

        // How long the oldest request that is still open (New or Contacted)
        // has been waiting — the number that says whether the queue is being
        // worked, which an average alone hides.
        public double? OldestOpenMinutes { get; set; }
    }
}
