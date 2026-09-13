using AlAmalBusiness.Application.DTOs.Email;

namespace AlAmalBusiness.Application.Services.Interface
{
    // Application-side abstraction over outgoing email, implemented in Api
    // (same arrangement as ILeadNotifier) so Application carries no SMTP
    // package. Enqueue never blocks and never talks to the mail server — the
    // request that triggered the email has already finished its own write and
    // must not wait on, or fail because of, SMTP. Returns false if the message
    // was dropped (queue full or email not configured).
    public interface IEmailQueue
    {
        bool Enqueue(EmailMessage message);

        // False when email isn't configured — Enqueue would drop everything.
        // The monthly report checks it before marking a month as sent.
        bool IsEnabled { get; }
    }
}
