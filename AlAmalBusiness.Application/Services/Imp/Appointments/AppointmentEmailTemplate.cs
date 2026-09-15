using AlAmalBusiness.Application.DTOs.Email;
using AlAmalBusiness.Domain.Models.Appointments;
using System.Net;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp.Appointments
{
    // The "new appointment request" email sent to every active address on the
    // notification emails list. Arabic and right-to-left, laid out like
    // FeedbackEmailTemplate so the two read as one system.
    //
    // Everything that came from the patient is HTML-encoded: the page is
    // anonymous, so its fields are untrusted input landing in a staff inbox.
    internal static class AppointmentEmailTemplate
    {
        public static EmailMessage Build(string to, AppointmentRequest appointment, string procedureName, string referralSourceName)
        {
            var phone = $"{appointment.PhoneCountryCode}{appointment.PhoneNumber}";
            var details = string.IsNullOrWhiteSpace(appointment.Details) ? "-" : appointment.Details;
            var received = appointment.CreatedDate.ToString("yyyy-MM-dd HH:mm");

            // The name is patient input, so line breaks are flattened before
            // it goes anywhere near a header.
            var subject = $"طلب موعد جديد - {procedureName} - {appointment.FullName}"
                .Replace('\r', ' ').Replace('\n', ' ');

            var rows = new (string Label, string Value, bool Ltr)[]
            {
                ("اسم المراجع", appointment.FullName, false),
                ("رقم الهاتف", phone, true),
                ("الإجراء", procedureName, false),
                ("كيف سمع عنا", referralSourceName, false),
                ("تاريخ الطلب", received, true)
            };

            var html = new StringBuilder();
            // Centered with an outer align="center" table rather than
            // margin:auto — Outlook ignores auto margins on a div.
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">");
            html.Append("<div dir=\"rtl\" style=\"max-width:600px;text-align:center;font-family:Tahoma,Arial,sans-serif;font-size:14px;color:#1f2937;\">");
            html.Append($"<h2 style=\"margin:0 0 12px;font-size:18px;\">وصل طلب موعد جديد — {Encode(procedureName)}</h2>");
            html.Append("<table align=\"center\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;margin:0 auto;\">");
            foreach (var (label, value, ltr) in rows)
            {
                html.Append("<tr>");
                html.Append($"<td style=\"font-weight:bold;border-bottom:1px solid #e5e7eb;white-space:nowrap;text-align:center;\">{Encode(label)}</td>");
                // The phone is wrapped in a tel: link so it can be dialled
                // straight from a phone's mail app.
                var cell = label == "رقم الهاتف"
                    ? $"<a href=\"tel:{Encode(phone)}\" style=\"color:#08517d;\">{Encode(value)}</a>"
                    : Encode(value);
                html.Append($"<td style=\"border-bottom:1px solid #e5e7eb;text-align:center;\" dir=\"{(ltr ? "ltr" : "auto")}\">{cell}</td>");
                html.Append("</tr>");
            }
            html.Append("</table>");
            html.Append("<h3 style=\"margin:16px 0 6px;font-size:15px;\">التفاصيل</h3>");
            html.Append($"<div dir=\"auto\" style=\"white-space:pre-wrap;text-align:center;background:#f9fafb;border:1px solid #e5e7eb;padding:10px;\">{Encode(details)}</div>");
            html.Append("<p style=\"margin-top:16px;color:#6b7280;font-size:12px;\">هذه رسالة تلقائية من صفحة حجز المواعيد في مستشفى الأمل، يرجى عدم الرد عليها.</p>");
            html.Append("</div>");
            html.Append("</td></tr></table>");

            var text = new StringBuilder();
            text.AppendLine($"وصل طلب موعد جديد — {procedureName}");
            text.AppendLine();
            foreach (var (label, value, _) in rows)
                text.AppendLine($"{label}: {value}");
            text.AppendLine();
            text.AppendLine("التفاصيل:");
            text.AppendLine(details);

            return new EmailMessage(to, subject, html.ToString(), text.ToString());
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
