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
        // Who the ticket is about — a list column, like the title.
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        // True when the ticket belongs to the insurance desk alone.
        public bool IsInsurance { get; set; }
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
        // Why it was raised — free text, possibly several lines. The ticket's
        // only prose since Description was removed on 2026-09-20.
        public string? Reason { get; set; }
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

    // Everything the New ticket form's pickers need, in one call.
    public class TicketFormOptionsResponse
    {
        public List<TicketOptionResponse> Categories { get; set; } = new();
        public List<TicketProcedureOptionResponse> Procedures { get; set; } = new();
    }

    // A procedure, plus whether picking it lets the raiser tick "insurance"
    // — true for "Open invoice" alone. The form reads this rather than
    // matching on the name, so the checkbox and the server's rule can't drift
    // apart when a procedure is renamed.
    public class TicketProcedureOptionResponse : TicketOptionResponse
    {
        public bool AllowsInsurance { get; set; }
    }

    public class TicketOptionResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    // What a solving agent's screen is handed the moment a ticket is raised
    // (ITicketNotifier). A list row plus the reason: the whole point of the
    // push is that the agent can read the ticket and act without opening
    // anything, so the one piece of prose it has travels with it. Never
    // broadcast — only the desk that owns the ticket receives it.
    public class TicketPushResponse : TicketListItemResponse
    {
        public string? Reason { get; set; }
        public string? PatientId { get; set; }
    }

    // What the raiser's screen is handed the moment their ticket is closed
    // (ITicketNotifier.TicketResolvedAsync). Sent to that one user's group
    // and to nobody else, so it may say what the ticket was about — enough
    // for a desktop notification to be worth reading on its own, without
    // the whole ticket travelling with it.
    public class TicketResolvedPush
    {
        // Who it goes to: the raiser. The notifier addresses their group
        // with it, which is why it travels on the push rather than being
        // looked up at the far end.
        public string? CreatedById { get; set; }
        public int TicketId { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        // "Success" or "Failed" — the same names the API serializes
        // everywhere else, not a bool, because the two read differently to
        // the person who raised it.
        public string? Status { get; set; }
        public bool Success { get; set; }
        // Who closed it, and (when it could not be solved) why.
        public string? ByName { get; set; }
        public string? Resolution { get; set; }
        public string? ProcedureName { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
