using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Email
{
    // One outgoing email to one recipient. Deliberately single-recipient: a
    // bad address rejected by the SMTP server then fails only its own
    // message, never everyone else's copy.
    //
    // Attachments are optional and held in memory until sent — keep them small
    // (the monthly questionnaire report's workbooks are tens of KB each).
    public sealed record EmailMessage(
        string To,
        string Subject,
        string HtmlBody,
        string TextBody,
        IReadOnlyList<EmailAttachment>? Attachments = null);

    public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType);
}
