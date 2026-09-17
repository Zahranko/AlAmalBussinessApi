namespace AlAmalBusiness.Domain.Constants
{
    // Where a support ticket stands. One open state and two terminals, each
    // stamping ClosedAt: Success (solved) and Failed (could not be solved,
    // and the closer has to say why — Ticket.Resolution).
    //
    // Ported as-is from the CRMS Tickets app, where a single "Closed" had
    // already been split into these two. Stored as int — append, never reorder.
    public enum TicketStatus
    {
        Open,
        Success,
        Failed
    }
}
