using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Domain.IRepositories.Tickets
{
    // The admin dashboard's period. Unlike the ticket lists there is no
    // visibility to resolve here — the dashboard is Admin-only, and an admin
    // reads every department and both sides of the queue.
    public class TicketStatsQuery
    {
        // Inclusive day bounds on when the ticket was RAISED, so one date
        // meaning runs through every number on the screen: a ticket raised
        // inside the period but closed after it still counts as the period's,
        // and its resolution time is part of the period's average.
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }
    }

    public class TicketStatusCountRow
    {
        public Constants.TicketStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class TicketNamedCountRow
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int OpenCount { get; set; }
    }

    // Per-agent closing figures, grouped on who closed the ticket
    // (AssignedToId, which closing is what sets).
    public class TicketCloserRow
    {
        public string? ActorId { get; set; }
        public string? ActorName { get; set; }
        public int ClosedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        // Raised -> closed, in minutes.
        public double? AvgResolutionMinutes { get; set; }
    }

    // One row per ticket that somebody other than its creator has touched:
    // who got there first, and how long they took. The per-agent response
    // average is built from these, so it credits whoever actually answered
    // rather than whoever happened to close it later.
    public class TicketFirstResponseRow
    {
        public string? ActorId { get; set; }
        public string? ActorName { get; set; }
        public double Minutes { get; set; }
    }

    public class TicketStatsRows
    {
        public List<TicketStatusCountRow> Statuses { get; set; } = new();
        public int InsuranceCount { get; set; }
        public List<TicketNamedCountRow> Departments { get; set; } = new();
        public List<TicketNamedCountRow> Procedures { get; set; } = new();
        public List<TicketNamedCountRow> Categories { get; set; } = new();
        public double? AvgResolutionMinutes { get; set; }
        // When the longest-waiting still-open ticket was raised; null when
        // nothing is open.
        public DateTime? OldestOpenAt { get; set; }
        public List<TicketCloserRow> Closers { get; set; } = new();
        public List<TicketFirstResponseRow> FirstResponses { get; set; } = new();
    }
}
