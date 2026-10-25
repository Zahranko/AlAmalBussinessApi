using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Tickets.Response
{
    public class MaintenanceTicketListItemResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
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
