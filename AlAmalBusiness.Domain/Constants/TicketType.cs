namespace AlAmalBusiness.Domain.Constants
{
    // Where a ticket was raised. General = the console. CertaCure = the
    // browser extension on top of the external CertaCure system, which fills
    // in Ticket.PatientId (the MRN) and SourceUrl (the CertaCure page it was
    // raised from). Stored as int — append, never reorder.
    public enum TicketType
    {
        General,
        CertaCure
    }
}
