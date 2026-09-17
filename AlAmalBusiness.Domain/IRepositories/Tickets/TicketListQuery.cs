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

        // ---- set by the service, never by the caller ----

        public TicketQueueScope Scope { get; set; } = TicketQueueScope.Open;

        // Narrows to tickets assigned to this user (Scope = Mine).
        public string? AssignedToId { get; set; }

        // Narrows to tickets this user raised (the created-by-me list). When
        // set, the two flags below and the scope are ignored — a creator
        // follows every ticket they raised, insurance ones included.
        public string? CreatedById { get; set; }

        // Which tickets the queue may show: the support team's (any payment
        // method but Insurance) and/or the insurance desk's (Insurance). An
        // actor holding neither sees an empty queue.
        public bool IncludeGeneral { get; set; }
        public bool IncludeInsurance { get; set; }
    }
}
