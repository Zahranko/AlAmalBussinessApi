using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Tickets.Response
{
    public class MaintenanceTicketListItemResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Site { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        // Set on a request from the public page, which has no account behind it.
        public string? RequesterName { get; set; }
        public string? RequesterPhone { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class MaintenanceTicketDetailResponse : MaintenanceTicketListItemResponse
    {
        public string? Details { get; set; }
        public string? Resolution { get; set; }
        public List<TicketHistoryResponse> History { get; set; } = new();
    }

    // What the public page gets back: the request number to quote when
    // following it up, and nothing of the stored record.
    public class MaintenancePublicSubmitResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public int? TicketId { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    // The maintenance dashboard (added 2026-11-07): TicketStatsResponse's
    // figures over the maintenance requests RAISED in the period, broken down
    // by building and priority. Admin and MaintenanceManager. Averages are
    // null, never 0, when there is nothing to average over.
    public class MaintenanceTicketStatsResponse
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public int Total { get; set; }
        public int OpenCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        // Share of closed requests that were fixed.
        public double SuccessPercent { get; set; }

        // Raised -> first comment or close by someone other than the raiser.
        public double? AvgResponseHours { get; set; }
        // Raised -> closed.
        public double? AvgResolutionHours { get; set; }
        // How long the longest-waiting open request has been waiting.
        public double? OldestOpenHours { get; set; }

        // The maintenance team, one row each.
        public List<TicketAgentStatResponse> Agents { get; set; } = new();
        // Name = the MaintenanceSite name, or null for "not given".
        public List<TicketBreakdownResponse> Sites { get; set; } = new();
        // Name = the MaintenancePriority name, Urgent first.
        public List<TicketBreakdownResponse> Priorities { get; set; } = new();
        public List<TicketBreakdownResponse> Departments { get; set; } = new();
    }

    // NotFound kept apart from a plain failure: 404 for a request the caller
    // may not see, 409 for a rule they broke.
    public class MaintenanceTicketActionResponse
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public MaintenanceTicketDetailResponse? Ticket { get; set; }
    }
}
