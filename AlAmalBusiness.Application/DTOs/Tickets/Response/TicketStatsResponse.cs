using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Tickets.Response
{
    // The ticket dashboard: how the queue is going, over the tickets RAISED
    // in the period. Admin-only (TicketController.CanReport).
    //
    // Every average is null, never 0, when there is nothing to average over:
    // "nobody has answered anything yet" and "everyone answered instantly"
    // are different facts and the screen shows them differently.
    public class TicketStatsResponse
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        // Of the total, how many went to the insurance desk.
        public int InsuranceCount { get; set; }

        // Share of closed tickets that were solved rather than given up on.
        public double SuccessPercent { get; set; }

        // Raised -> first reply by somebody other than the person who raised
        // it. This is the number staff actually feel: how long a ticket sat
        // before anyone answered.
        public double? AvgResponseHours { get; set; }
        // Raised -> closed.
        public double? AvgResolutionHours { get; set; }
        // How long the longest-waiting still-open ticket has been waiting.
        public double? OldestOpenHours { get; set; }

        public List<TicketAgentStatResponse> Agents { get; set; } = new();
        public List<TicketBreakdownResponse> Departments { get; set; } = new();
        public List<TicketBreakdownResponse> Procedures { get; set; } = new();
        public List<TicketBreakdownResponse> Categories { get; set; } = new();
    }

    // One person's numbers. Responses and closes are counted separately on
    // purpose: whoever answers a ticket first is often not whoever closes it,
    // and crediting both to the closer would flatter the wrong person.
    public class TicketAgentStatResponse
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }

        public int ClosedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public double? AvgResolutionHours { get; set; }

        // Tickets this person answered first, and how long they took to.
        public int RespondedCount { get; set; }
        public double? AvgResponseHours { get; set; }
    }

    public class TicketBreakdownResponse
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int ClosedCount { get; set; }
    }
}
