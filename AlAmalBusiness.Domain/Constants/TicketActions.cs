namespace AlAmalBusiness.Domain.Constants
{
    // Timeline entry kind, the ticket counterpart of AppointmentActions.
    // Stored as int — append, never reorder.
    //
    // The CRMS original also carried Claimed and Closed (retired there before
    // the port) and a review desk's ReviewAccepted/ReviewDeclined (dropped
    // with the invoice review, 2026-09-17). No rows were imported, so none of
    // them came across.
    public enum TicketActions
    {
        Created,
        Commented,
        // Closed with this outcome. A Failed entry carries the reason as its note.
        Success,
        Failed,
        Reopened
    }
}
