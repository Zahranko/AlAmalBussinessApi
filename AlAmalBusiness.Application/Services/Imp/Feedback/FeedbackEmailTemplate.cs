using AlAmalBusiness.Application.DTOs.Email;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.Feedback;
using System.Net;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp.Feedback
{
    // The "new message for your department" email a department's FManager
    // gets when a patient submits the public form. Arabic and right-to-left —
    // it is read by hospital staff, like the rest of the feedback screens.
    //
    // Everything that came from the patient is HTML-encoded: the form is
    // anonymous, so its fields are untrusted input landing in a staff inbox.
    internal static class FeedbackEmailTemplate
    {
        public static EmailMessage Build(string to, PatientFeedback feedback, string? departmentName)
        {
            var typeLabel = TypeLabel(feedback.Type);
            var department = string.IsNullOrWhiteSpace(departmentName) ? "-" : departmentName;
            var name = string.IsNullOrWhiteSpace(feedback.FullName) ? "-" : feedback.FullName;
            var phone = string.IsNullOrWhiteSpace(feedback.PhoneNumber) ? "-" : $"{feedback.PhoneCountryCode}{feedback.PhoneNumber}";
            var details = string.IsNullOrWhiteSpace(feedback.Details) ? "-" : feedback.Details;
            var visitDate = feedback.VisitDate.ToString("yyyy-MM-dd");
            var received = feedback.CreatedDate.ToString("yyyy-MM-dd HH:mm");

            var subject = $"[{typeLabel}] رسالة جديدة لقسم {department} - {feedback.ReferenceNumber}";

            var rows = new (string Label, string Value)[]
            {
                ("الرقم المرجعي", feedback.ReferenceNumber),
                ("النوع", typeLabel),
                ("القسم", department),
                ("اسم المراجع", name),
                ("رقم الهاتف", phone),
                ("تاريخ الزيارة", visitDate),
                ("تاريخ الاستلام", received)
            };

            var html = new StringBuilder();
            // Centered with an outer align="center" table rather than
            // margin:auto — Outlook ignores auto margins on a div.
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\"><tr><td align=\"center\">");
            html.Append("<div dir=\"rtl\" style=\"max-width:600px;text-align:center;font-family:Tahoma,Arial,sans-serif;font-size:14px;color:#1f2937;\">");
            html.Append($"<h2 style=\"margin:0 0 12px;font-size:18px;\">وصلت رسالة {Encode(typeLabel)} جديدة لقسم {Encode(department)}</h2>");
            html.Append("<table align=\"center\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;margin:0 auto;\">");
            foreach (var (label, value) in rows)
            {
                html.Append("<tr>");
                html.Append($"<td style=\"font-weight:bold;border-bottom:1px solid #e5e7eb;white-space:nowrap;text-align:center;\">{Encode(label)}</td>");
                html.Append($"<td style=\"border-bottom:1px solid #e5e7eb;text-align:center;\" dir=\"auto\">{Encode(value)}</td>");
                html.Append("</tr>");
            }
            html.Append("</table>");
            html.Append("<h3 style=\"margin:16px 0 6px;font-size:15px;\">التفاصيل</h3>");
            html.Append($"<div dir=\"auto\" style=\"white-space:pre-wrap;text-align:center;background:#f9fafb;border:1px solid #e5e7eb;padding:10px;\">{Encode(details)}</div>");
            html.Append("<p style=\"margin-top:16px;color:#6b7280;font-size:12px;\">هذه رسالة تلقائية من نظام ملاحظات المراجعين في مستشفى الأمل، يرجى عدم الرد عليها.</p>");
            html.Append("</div>");
            html.Append("</td></tr></table>");

            var text = new StringBuilder();
            text.AppendLine($"وصلت رسالة {typeLabel} جديدة لقسم {department}");
            text.AppendLine();
            foreach (var (label, value) in rows)
                text.AppendLine($"{label}: {value}");
            text.AppendLine();
            text.AppendLine("التفاصيل:");
            text.AppendLine(details);

            return new EmailMessage(to, subject, html.ToString(), text.ToString());
        }

        private static string TypeLabel(FeedbackType type) => type switch
        {
            FeedbackType.Thanks => "شكر",
            FeedbackType.Suggestion => "اقتراح",
            FeedbackType.Complaint => "شكوى",
            _ => type.ToString()
        };

        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
