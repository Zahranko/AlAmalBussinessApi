namespace AlAmalBusiness.Domain.Constants
{
    // Where a ticket was raised. General = the console. CertaCure = the
    // browser extension on top of the external CertaCure system, which fills
    // in Ticket.PatientId/SourceUrl. That extension was never built for the
    // CRMS app either; the columns came across so it needs no schema change
    // when it is. Stored as int — append, never reorder.
    public enum TicketType
    {
        General,
        CertaCure
    }
}
