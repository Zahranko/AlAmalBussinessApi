using AlAmalBusiness.Application.DTOs.Tickets.Response;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Tickets
{
    // The live push that lands on a solving agent's screen the moment a
    // ticket is raised — the CertaCure extension's agent panel listens to
    // it, and the email stays as the record for whoever wasn't at the
    // browser. Implemented in the Api project (over SignalR) so Application
    // stays free of any real-time-transport package reference, exactly as
    // ILeadNotifier is.
    //
    // Unlike ILeadNotifier this is NOT a broadcast: a ticket carries a
    // patient's MRN and the reason it was raised, so it goes to the desk
    // that has to solve it and nobody else. Which desk that is follows the
    // same flag the emails follow, so `isInsurance` travels with every call
    // rather than being re-derived at the far end.
    public interface ITicketNotifier
    {
        Task TicketCreatedAsync(TicketPushResponse ticket);

        // A ticket left the queue (closed) or came back into it (reopened),
        // so every other agent's panel can drop or re-add it instead of
        // showing work somebody else already did.
        Task TicketChangedAsync(int ticketId, bool isInsurance, string status, string? byName);

        // The other direction: a ticket was closed, so tell the person who
        // raised it. This one goes to that user alone, not to a desk — it is
        // the live twin of the "your ticket was closed" email, for whoever
        // is at the browser when it happens, and it carries the outcome and
        // the reason so the notification says what happened without anything
        // being opened.
        Task TicketResolvedAsync(TicketResolvedPush resolved);
    }
}
