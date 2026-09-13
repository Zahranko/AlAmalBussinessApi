namespace AlAmalBusiness.Application.DTOs.Email
{
    // One outgoing email to one recipient. Deliberately single-recipient: a
    // bad address rejected by the SMTP server then fails only its own
    // message, never everyone else's copy.
    public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);
}
