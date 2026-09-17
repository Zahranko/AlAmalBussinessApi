using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Tickets.Response
{
    // One row of either ticket list (the queue, created-by-me).
    // Enums travel as strings, like every response in this API.
    public class TicketListItemResponse
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        public int? ReasonId { get; set; }
        public string? ReasonName { get; set; }
        public string? PaymentMethod { get; set; }
        // Only set for CertaCure tickets — the page it was raised from.
        public string? SourceUrl { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // The detail view: the row, the free text and the timeline.
    public class TicketDetailResponse : TicketListItemResponse
    {
        public string? Description { get; set; }
        public string? PatientId { get; set; }
        public string? Resolution { get; set; }
        public List<TicketHistoryResponse> History { get; set; } = new();
    }

    public class TicketHistoryResponse
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Result wrapper for create and the workflow actions. NotFound is kept
    // apart from a plain failure so the controller answers 404 for a ticket
    // the caller may not see, rather than a 409 confirming the id exists.
    public class TicketActionResponse
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Error { get; set; }
        public TicketDetailResponse? Ticket { get; set; }
    }

    // Everything the New ticket form's pickers need, in one call. Reasons
    // carry their procedure id so the form cascades without another request.
    public class TicketFormOptionsResponse
    {
        public List<TicketOptionResponse> Categories { get; set; } = new();
        public List<TicketOptionResponse> Procedures { get; set; } = new();
        public List<TicketReasonOptionResponse> Reasons { get; set; } = new();
    }

    public class TicketOptionResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TicketReasonOptionResponse : TicketOptionResponse
    {
        public int ProcedureId { get; set; }
    }
}
