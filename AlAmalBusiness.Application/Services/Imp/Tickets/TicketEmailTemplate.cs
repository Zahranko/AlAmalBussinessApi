using AlAmalBusiness.Application.DTOs.Email;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp.Tickets
{
    // The ticket emails. They replace the CRMS app's in-app bell; this app
    // has no notification inbox, so they go out the way feedback and
    // appointment notices do. Arabic and
    // right-to-left, laid out like AppointmentEmailTemplate so every email
    // the hospital's staff get reads as one system.
    //
    // Ticket text is staff input rather than a patient's, but it is still
    // HTML-encoded everywhere — a title is not markup.
    internal static class TicketEmailTemplate
    {
        public record TicketFacts(int Id, string Title, string? CreatedBy, string? Department, string? PaymentMethod, string Created);

        // A ticket was raised — to the support team, or for an Insurance
        // ticket to the insurance desk that alone works it.
        public static EmailMessage NewTicket(string to, TicketFacts ticket, bool insurance, string? link) =>
            Build(to, ticket, link,
                subject: insurance ? $"تذكرة تأمين جديدة - {ticket.Title}" : $"تذكرة جديدة في قائمة الدعم - {ticket.Title}",
                heading: insurance ? "وصلت تذكرة تأمين جديدة إلى مكتب التأمين" : "وصلت تذكرة جديدة إلى قائمة الدعم",
                noteLabel: null, note: null);

        // To the creator: their ticket was closed.
        public static EmailMessage Closed(string to, TicketFacts ticket, bool success, string closedBy, string? reason, string? link) =>
            Build(to, ticket, link,
                subject: $"تم إغلاق تذكرتك ({(success ? "تم الحل" : "تعذّر الحل")}) - {ticket.Title}",
                heading: success ? $"أغلق {closedBy} تذكرتك: تم الحل" : $"أغلق {closedBy} تذكرتك: تعذّر الحل",
                noteLabel: success ? null : "السبب", note: reason);

        private static EmailMessage Build(
            string to, TicketFacts ticket, string? link, string subject, string heading, string? noteLabel, string? note)
        {
            // Line breaks flattened before the title goes anywhere near a header.
            subject = subject.Replace('\r', ' ').Replace('\n', ' ');

            var rows = new List<(string Label, string Value, bool Ltr)>
            {
                ("رقم التذكرة", $"#{ticket.Id}", true),
                ("العنوان", ticket.Title, false),
                ("أنشأها", string.IsNullOrWhiteSpace(ticket.CreatedBy) ? "-" : ticket.CreatedBy, false),
                ("القسم", string.IsNullOrWhiteSpace(ticket.Department) ? "-" : ticket.Department, false)
            };
            if (!string.IsNullOrWhiteSpace(ticket.PaymentMethod))
                rows.Add(("طريقة الدفع", ticket.PaymentMethod, false));
            rows.Add(("تاريخ الإنشاء", ticket.Created, true));

            var html = new StringBuilder();
            // Centered with an outer align="center" table rather than
            // margin:auto — Outlook ignores auto margins on a div.
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">");
            html.Append("<div dir=\"rtl\" style=\"max-width:600px;text-align:center;font-family:Tahoma,Arial,sans-serif;font-size:14px;color:#1f2937;\">");
            html.Append($"<h2 style=\"margin:0 0 12px;font-size:18px;\">{Encode(heading)}</h2>");
            html.Append("<table align=\"center\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;margin:0 auto;\">");
            foreach (var (label, value, ltr) in rows)
            {
                html.Append("<tr>");
                html.Append($"<td style=\"font-weight:bold;border-bottom:1px solid #e5e7eb;white-space:nowrap;text-align:center;\">{Encode(label)}</td>");
                html.Append($"<td style=\"border-bottom:1px solid #e5e7eb;text-align:center;\" dir=\"{(ltr ? "ltr" : "auto")}\">{Encode(value)}</td>");
                html.Append("</tr>");
            }
            html.Append("</table>");
            if (noteLabel != null && !string.IsNullOrWhiteSpace(note))
            {
                html.Append($"<h3 style=\"margin:16px 0 6px;font-size:15px;\">{Encode(noteLabel)}</h3>");
                html.Append($"<div dir=\"auto\" style=\"white-space:pre-wrap;text-align:center;background:#f9fafb;border:1px solid #e5e7eb;padding:10px;\">{Encode(note)}</div>");
            }
            if (link != null)
            {
                // A table-cell button with a bgcolor, not a styled <a> alone —
                // Outlook drops padding and background on inline links.
                html.Append("<table role=\"presentation\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:20px auto 0;\"><tr>");
                html.Append("<td align=\"center\" bgcolor=\"#0f766e\" style=\"border-radius:6px;\">");
                html.Append($"<a href=\"{Encode(link)}\" target=\"_blank\" style=\"display:inline-block;padding:10px 24px;font-family:Tahoma,Arial,sans-serif;font-size:14px;font-weight:bold;color:#ffffff;text-decoration:none;border-radius:6px;\">فتح التذكرة</a>");
                html.Append("</td></tr></table>");
            }
            html.Append("<p style=\"margin-top:16px;color:#6b7280;font-size:12px;\">هذه رسالة تلقائية من نظام التذاكر في مستشفى الأمل، يرجى عدم الرد عليها.</p>");
            html.Append("</div>");
            html.Append("</td></tr></table>");

            var text = new StringBuilder();
            text.AppendLine(heading);
            text.AppendLine();
            foreach (var (label, value, _) in rows)
                text.AppendLine($"{label}: {value}");
            if (noteLabel != null && !string.IsNullOrWhiteSpace(note))
            {
                text.AppendLine();
                text.AppendLine($"{noteLabel}:");
                text.AppendLine(note);
            }
            if (link != null)
            {
                text.AppendLine();
                text.AppendLine($"فتح التذكرة: {link}");
            }

            return new EmailMessage(to, subject, html.ToString(), text.ToString());
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
