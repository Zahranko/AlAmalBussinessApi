using AlAmalBusiness.Domain.Constants;

namespace AlAmalBusiness.Domain.IRepositories.Tickets
{
    // The queue's tabs. Open is the default: the queue is what is still
    // waiting to be solved.
    public enum TicketQueueScope
    {
        // Every open ticket.
        Open,
        // Closed by the caller (closing is what assigns a ticket). Every status.
        Mine,
        // Open and nobody's.
        Unassigned,
        // Success or Failed, most recently closed first.
        Closed
    }

    // The raiser's own list (created-by-me), added 2026-10-26: still open,
    // everything, or closed either way. Open is the default — what a raiser
    // opens the page to check is what is still waiting.
    public enum TicketRaisedView
    {
        Open,
        All,
        Closed
    }

    // One filter object for both ticket lists (the queue and created-by-me),
    // passed controller -> service -> repository so adding a filter doesn't
    // ripple through three signatures. Mirrors AppointmentListQuery.
    public class TicketListQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        // Matches the title.
        public string? Search { get; set; }
        // An explicit status always wins over what the scope would show.
        public TicketStatus? Status { get; set; }
        public int? CategoryId { get; set; }

        // Work order rather than news order: the ticket that has been
        // waiting longest first. The console's list is a list — you read the
        // newest at the top — but a queue worked one ticket at a time is
        // FIFO, or the oldest ticket starves while newer ones keep arriving
        // in front of it. The CertaCure extension's agent panel asks for
        // this; nothing else does. Ignored by the Closed tab, which is
        // ordered by when each ticket was closed and means "what just
        // happened".
        public bool OldestFirst { get; set; }

        // Which of the caller's own tickets the created-by-me list shows.
        // Ignored by the queue, which has its scope instead.
        public TicketRaisedView RaisedView { get; set; } = TicketRaisedView.Open;

        // ---- set by the service, never by the caller ----

        public TicketQueueScope Scope { get; set; } = TicketQueueScope.Open;

        // Narrows to tickets assigned to this user (Scope = Mine).
        public string? AssignedToId { get; set; }

        // Narrows to tickets this user raised (the created-by-me list). When
        // set, the visibility below and the scope are ignored — a creator
        // follows every ticket they raised, insurance ones included.
        public string? CreatedById { get; set; }

        // What the queue may show, resolved by the service from the caller's
        // roles. These are OR-ed together, not AND-ed: someone who is both a
        // manager and the insurance desk sees the union of the two. An actor
        // with none of them sees only the tickets they raised.
        //
        // Unrestricted is Admin alone.
        public bool SeesEverything { get; set; }
        // Every ticket not flagged insurance, from any department (TSupport).
        public bool SeesSupportQueue { get; set; }
        // Every ticket flagged insurance, from any department (TInsurance).
        public bool SeesInsurance { get; set; }
        // Every not-flagged ticket raised inside this department (TManager).
        // Null means no department view at all.
        public int? SeesDepartmentId { get; set; }
        // The caller, so their own tickets are always in their queue
        // alongside whatever the flags above let them see.
        public string? ViewerId { get; set; }

        // Parks delayed tickets after every other open one. Only for whoever
        // may delay (TSupport, Admin) — the flag is their queue-ordering
        // tool, and a raiser's own list keeps its plain order (2026-10-26).
        public bool DelayedLast { get; set; }
    }
}
