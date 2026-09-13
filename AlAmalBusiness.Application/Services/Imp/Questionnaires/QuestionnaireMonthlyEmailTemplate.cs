using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    // The monthly questionnaire report a department's QManagers get: Arabic,
    // right-to-left, and built from tables with inline styles only — the same
    // constraints FeedbackEmailTemplate works under (Outlook ignores most CSS).
    //
    // The "graphs" are table-cell bars, not images: mail clients block remote
    // images and many strip inline ones, but a coloured cell with a width
    // renders everywhere. The attached workbook carries the real charts.
    internal static class QuestionnaireMonthlyEmailTemplate
    {
        private const string Navy = "#08517d";
        private const string Green = "#159f8a";
        private const string Muted = "#6b7280";
        private const string Track = "#eef2f7";
        private const string Faded = "#9cc3d9";
        private const string FadedGreen = "#a3d9cf";

        private static readonly string[] MonthNames =
        [
            "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران",
            "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول"
        ];

        public static string MonthName(int year, int month) => $"{MonthNames[month - 1]} {year}";

        public static string Subject(DepartmentMonthlyReport report) =>
            $"تقرير الاستبيانات الشهري - {report.DepartmentName} - {MonthName(report.Year, report.Month)}";

        // `consoleBaseUrl` null leaves the "open in the console" buttons out.
        public static string Html(DepartmentMonthlyReport report, string? consoleBaseUrl)
        {
            var html = new StringBuilder();
            html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" bgcolor=\"#f3f6f9\"><tr><td align=\"center\" style=\"padding:20px 10px;\">");
            html.Append("<table role=\"presentation\" width=\"640\" cellpadding=\"0\" cellspacing=\"0\" dir=\"rtl\" style=\"max-width:640px;width:100%;background:#ffffff;font-family:Tahoma,Arial,sans-serif;font-size:14px;color:#1f2937;border-radius:10px;\">");

            // Header band
            html.Append($"<tr><td bgcolor=\"{Navy}\" style=\"padding:22px 24px;border-radius:10px 10px 0 0;color:#ffffff;text-align:right;\">");
            html.Append("<div style=\"font-size:12px;opacity:.85;\">مستشفى الأمل</div>");
            html.Append($"<div style=\"font-size:20px;font-weight:bold;margin-top:4px;\">تقرير الاستبيانات الشهري — {Encode(MonthName(report.Year, report.Month))}</div>");
            html.Append($"<div style=\"font-size:14px;margin-top:4px;\">قسم {Encode(report.DepartmentName)}</div>");
            html.Append("</td></tr>");

            html.Append("<tr><td style=\"padding:20px 24px;text-align:right;\">");
            html.Append($"<p style=\"margin:0 0 14px;color:{Muted};font-size:13px;\">ملخص تقييمات المرضى لهذا الشهر مقارنةً بجميع الأشهر السابقة. ملف Excel بالرسوم البيانية الكاملة مرفق مع هذه الرسالة.</p>");

            // Department totals
            html.Append(SectionTitle("إجمالي القسم"));
            html.Append(KpiRow(report.Totals));

            foreach (var q in report.Questionnaires)
            {
                html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin-top:26px;border-top:2px solid #e5e7eb;\"><tr><td style=\"padding-top:18px;\"></td></tr></table>");
                html.Append($"<div style=\"font-size:17px;font-weight:bold;color:{Navy};\">{Encode(q.Title)}{(q.IsActive ? "" : $" <span style=\"font-size:12px;color:{Muted};font-weight:normal;\">(متوقف)</span>")}</div>");
                html.Append($"<div style=\"font-size:12px;color:{Muted};margin:2px 0 12px;\" dir=\"ltr\">/{Encode(q.Slug)}</div>");

                html.Append(KpiRow(q.Trend));

                if (q.Trend.ReportMonth.Submissions == 0 && q.Trend.PreviousMonths.Submissions == 0)
                {
                    html.Append($"<p style=\"color:{Muted};\">لا توجد ردود على هذا الاستبيان بعد.</p>");
                }
                else
                {
                    html.Append(SubTitle("متوسط التقييم شهرياً (من 5)"));
                    html.Append(MonthBars(q.Trend, m => m.AverageRating, 5, v => v.ToString("0.00", CultureInfo.InvariantCulture), Navy, Faded));

                    html.Append(SubTitle("نسبة الرضا شهرياً (جيد أو جيد جداً)"));
                    html.Append(MonthBars(q.Trend, m => m.SatisfactionPercent, 100, v => $"{v:0.#}%\u200E", Green, FadedGreen));

                    html.Append(SubTitle("الأسئلة: هذا الشهر مقابل الأشهر السابقة"));
                    html.Append(QuestionTable(q.Questions));
                }

                if (consoleBaseUrl != null)
                {
                    var link = $"{consoleBaseUrl}/q/questionnaires/{q.Id}";
                    html.Append("<table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin-top:14px;\"><tr>");
                    html.Append($"<td bgcolor=\"{Navy}\" style=\"border-radius:6px;\"><a href=\"{Encode(link)}\" target=\"_blank\" style=\"display:inline-block;padding:9px 20px;font-family:Tahoma,Arial,sans-serif;font-size:13px;font-weight:bold;color:#ffffff;text-decoration:none;\">عرض النتائج في النظام</a></td>");
                    html.Append("</tr></table>");
                }
            }

            html.Append($"<p style=\"margin-top:26px;color:{Muted};font-size:12px;\">هذه رسالة تلقائية من نظام استبيانات المرضى في مستشفى الأمل، تُرسل في بداية كل شهر لمديري الاستبيانات في القسم. يرجى عدم الرد عليها.</p>");
            html.Append("</td></tr></table>");
            html.Append("</td></tr></table>");
            return html.ToString();
        }

        public static string Text(DepartmentMonthlyReport report)
        {
            var text = new StringBuilder();
            text.AppendLine($"تقرير الاستبيانات الشهري - {MonthName(report.Year, report.Month)} - قسم {report.DepartmentName}");
            text.AppendLine();
            AppendTextKpis(text, "إجمالي القسم", report.Totals);
            foreach (var q in report.Questionnaires)
            {
                text.AppendLine();
                AppendTextKpis(text, q.Title, q.Trend);
            }
            text.AppendLine();
            text.AppendLine("ملف Excel بالرسوم البيانية مرفق مع هذه الرسالة.");
            return text.ToString();
        }

        // ---------- pieces ----------

        private static string SectionTitle(string title) =>
            $"<div style=\"font-size:15px;font-weight:bold;margin:4px 0 10px;\">{Encode(title)}</div>";

        private static string SubTitle(string title) =>
            $"<div style=\"font-size:13px;font-weight:bold;margin:18px 0 8px;\">{Encode(title)}</div>";

        // Three tiles: responses, average, satisfied — each with its change
        // against every month before.
        private static string KpiRow(QuestionnaireTrend t)
        {
            var now = t.ReportMonth;
            var before = t.PreviousMonths;
            var sb = new StringBuilder();
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"6\" style=\"margin:0 -6px;\"><tr>");
            sb.Append(Kpi("الردود هذا الشهر", now.Submissions.ToString(CultureInfo.InvariantCulture),
                before.Submissions == 0 ? "لا توجد أشهر سابقة" : $"متوسط الأشهر السابقة: {AverageMonthly(t):0.#}"));
            sb.Append(Kpi("متوسط التقييم", now.AverageRating is double a ? a.ToString("0.00", CultureInfo.InvariantCulture) : "—",
                Delta(now.AverageRating, before.AverageRating, v => v.ToString("0.00", CultureInfo.InvariantCulture), before.AverageRating is double b ? $"قبل: {b:0.00}" : null)));
            sb.Append(Kpi("نسبة الرضا", now.SatisfactionPercent is double s ? $"{s:0.#}%\u200E" : "—",
                Delta(now.SatisfactionPercent, before.SatisfactionPercent, v => $"{v:0.#} نقطة", before.SatisfactionPercent is double p ? $"قبل: {p:0.#}%\u200E" : null)));
            sb.Append("</tr></table>");
            return sb.ToString();
        }

        private static string Kpi(string label, string value, string footHtml) =>
            "<td width=\"33%\" valign=\"top\" style=\"background:#f8fafc;border:1px solid #e5e7eb;border-radius:8px;padding:10px 12px;text-align:right;\">" +
            $"<div style=\"font-size:12px;color:{Muted};\">{Encode(label)}</div>" +
            $"<div style=\"font-size:22px;font-weight:bold;margin:2px 0;\">{Encode(value)}</div>" +
            $"<div style=\"font-size:12px;\">{footHtml}</div></td>";

        // Average responses per month over the months before the report month
        // that are on the chart, so the comparison is like for like.
        private static double AverageMonthly(QuestionnaireTrend t)
        {
            var earlier = t.Months.Count - 1;
            if (earlier <= 0) return 0;
            var sum = 0;
            for (var i = 0; i < earlier; i++) sum += t.Months[i].Submissions;
            return (double)sum / earlier;
        }

        // ▲ green / ▼ red / = grey, already HTML.
        private static string Delta(double? now, double? before, Func<double, string> format, string? beforeLabel)
        {
            if (now is not double n || before is not double b)
                return $"<span style=\"color:{Muted};\">{Encode(beforeLabel ?? "لا توجد بيانات سابقة")}</span>";

            var d = Math.Round(n - b, 2);
            var (arrow, color) = d > 0 ? ("▲", "#15803d") : d < 0 ? ("▼", "#b91c1c") : ("=", Muted);
            return $"<span style=\"color:{color};font-weight:bold;\">{arrow} {Encode(format(Math.Abs(d)))}</span> <span style=\"color:{Muted};\">{Encode(beforeLabel ?? "")}</span>";
        }

        // One row per month: name | bar | value. The report month is the
        // strong colour and bold; earlier months are faded.
        private static string MonthBars(QuestionnaireTrend t, Func<MonthPointResponse, double?> pick, double max, Func<double, string> format, string strong, string faded)
        {
            var sb = new StringBuilder();
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\">");
            for (var i = 0; i < t.Months.Count; i++)
            {
                var m = t.Months[i];
                var isReport = i == t.Months.Count - 1;
                var value = pick(m);
                var pct = value is double v ? (int)Math.Round(Math.Clamp(v / max, 0, 1) * 100) : 0;
                var weight = isReport ? "bold" : "normal";

                sb.Append("<tr>");
                sb.Append($"<td width=\"110\" style=\"font-size:12px;padding:3px 0 3px 8px;white-space:nowrap;font-weight:{weight};\">{Encode(MonthName(m.Year, m.Month))}</td>");
                sb.Append("<td style=\"padding:3px 0;\">");
                sb.Append($"<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" bgcolor=\"{Track}\" style=\"border-radius:4px;\"><tr>");
                if (pct > 0)
                    sb.Append($"<td width=\"{pct}%\" bgcolor=\"{(isReport ? strong : faded)}\" style=\"height:14px;font-size:0;line-height:0;border-radius:4px;\">&nbsp;</td>");
                if (pct < 100)
                    sb.Append("<td style=\"height:14px;font-size:0;line-height:0;\">&nbsp;</td>");
                sb.Append("</tr></table></td>");
                sb.Append($"<td width=\"58\" style=\"font-size:12px;padding:3px 8px 3px 0;text-align:left;font-weight:{weight};\" dir=\"ltr\">{(value is double x ? Encode(format(x)) : "—")}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private static string QuestionTable(List<QuestionCompareRow> rows)
        {
            var th = "style=\"background:#eef2ff;padding:7px 6px;font-size:12px;border-bottom:1px solid #e5e7eb;\"";
            var td = "style=\"padding:7px 6px;font-size:12px;border-bottom:1px solid #f1f5f9;text-align:center;\"";
            var sb = new StringBuilder();
            sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;\">");
            sb.Append($"<tr><th {th} align=\"right\">السؤال</th><th {th}>هذا الشهر</th><th {th}>قبل</th><th {th}>التغيّر</th><th {th}>الرضا</th></tr>");
            foreach (var r in rows)
            {
                sb.Append("<tr>");
                sb.Append($"<td style=\"padding:7px 6px;font-size:12px;border-bottom:1px solid #f1f5f9;text-align:right;\">{Encode(r.Text)}</td>");
                sb.Append($"<td {td}><b>{Fmt(r.AverageThisMonth)}</b></td>");
                sb.Append($"<td {td}>{Fmt(r.AverageBefore)}</td>");
                sb.Append($"<td {td}>{Delta(r.AverageThisMonth, r.AverageBefore, v => v.ToString("0.00", CultureInfo.InvariantCulture), null)}</td>");
                sb.Append($"<td {td}>{(r.SatisfiedThisMonth is double s ? $"{s:0.#}%\u200E" : "—")}</td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return sb.ToString();
        }

        private static void AppendTextKpis(StringBuilder text, string title, QuestionnaireTrend t)
        {
            text.AppendLine(title);
            text.AppendLine($"  الردود هذا الشهر: {t.ReportMonth.Submissions}");
            text.AppendLine($"  متوسط التقييم: {Fmt(t.ReportMonth.AverageRating)} (الأشهر السابقة: {Fmt(t.PreviousMonths.AverageRating)})");
            text.AppendLine($"  نسبة الرضا: {Pct(t.ReportMonth.SatisfactionPercent)} (الأشهر السابقة: {Pct(t.PreviousMonths.SatisfactionPercent)})");
        }

        private static string Fmt(double? v) => v is double x ? x.ToString("0.00", CultureInfo.InvariantCulture) : "—";
        private static string Pct(double? v) => v is double x ? $"{x:0.#}%\u200E" : "—";
        private static string Encode(string value) => WebUtility.HtmlEncode(value);
    }
}
