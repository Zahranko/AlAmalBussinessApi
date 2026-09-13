using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    // Questionnaire results as workbooks, in the feedback report's style
    // (FeedbackExcelReportService): a bold title, an italic period line, a
    // tinted header row, centred values — plus native Excel charts bound to
    // the cells (ExcelChartWriter).
    //
    // Scoping is never decided here — QuestionnaireService has already pinned
    // a QManager to their own department before any of this runs.
    public class QuestionnaireExcelReportService : IQuestionnaireExcelReportService
    {
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#EEF2FF");

        private const string AverageColor = "08517D";
        private const string SatisfiedColor = "159F8A";
        private const string ResponsesColor = "169FD9";

        private const string AverageFormat = "0.00";
        private const string PercentFormat = "0.0%";

        private static readonly (QuestionRating Rating, string Label)[] Ratings =
        [
            (QuestionRating.VeryGood, "Very good"),
            (QuestionRating.Good, "Good"),
            (QuestionRating.Mid, "Mid"),
            (QuestionRating.Bad, "Bad"),
            (QuestionRating.VeryBad, "Very bad")
        ];

        public byte[] Build(QuestionnaireExportData data)
        {
            var charts = new List<ChartSpec>();
            using var workbook = new XLWorkbook();

            WriteSummary(workbook.Worksheets.Add("Summary"), data);
            WriteQuestions(workbook.Worksheets.Add("By Question"), data.Stats, charts);
            WriteTrend(workbook.Worksheets.Add("Trend"), data.Trend, charts);
            WriteResponses(workbook.Worksheets.Add("Responses"), data);

            return ExcelChartWriter.AddCharts(Save(workbook), charts);
        }

        public byte[] BuildOverview(QuestionnaireListResponse list)
        {
            var charts = new List<ChartSpec>();
            using var workbook = new XLWorkbook();
            const string sheetName = "Questionnaires";
            var sheet = workbook.Worksheets.Add(sheetName);

            WriteTitle(sheet, "Al Amal Hospital - Patient Questionnaires", list.From, list.To);

            var row = 4;
            sheet.Cell(row, 1).Value = "Questionnaires";
            sheet.Cell(row++, 2).Value = $"{list.QuestionnaireCount} ({list.ActiveCount} active)";
            sheet.Cell(row, 1).Value = "Responses";
            sheet.Cell(row++, 2).Value = list.TotalSubmissions;
            sheet.Cell(row, 1).Value = "Average rating (out of 5)";
            WriteAverage(sheet.Cell(row++, 2), list.AverageRating);
            sheet.Cell(row, 1).Value = "Satisfied (Good or Very good)";
            WritePercent(sheet.Cell(row++, 2), list.SatisfactionPercent);
            sheet.Range(4, 1, row - 1, 1).Style.Font.Bold = true;
            Center(sheet.Range(4, 2, row - 1, 2));

            row++;
            string[] headers = ["Questionnaire", "Link", "Department", "Status", "Questions", "Responses", "Average", "Satisfied", "Last response"];
            var headerRow = row;
            WriteHeader(sheet, headerRow, headers);

            foreach (var q in list.Questionnaires)
            {
                row++;
                sheet.Cell(row, 1).Value = q.Title;
                sheet.Cell(row, 2).Value = "/" + q.Slug;
                sheet.Cell(row, 3).Value = q.DepartmentName ?? "-";
                sheet.Cell(row, 4).Value = q.IsActive ? "Active" : "Inactive";
                sheet.Cell(row, 5).Value = q.QuestionCount;
                sheet.Cell(row, 6).Value = q.SubmissionCount;
                WriteAverage(sheet.Cell(row, 7), q.AverageRating);
                WritePercent(sheet.Cell(row, 8), q.SatisfactionPercent);
                sheet.Cell(row, 9).Value = q.LastSubmissionDate?.ToString("yyyy-MM-dd HH:mm") ?? "-";
                CenterRow(sheet, row, headers.Length);
            }

            if (row == headerRow)
            {
                WriteNoData(sheet, ref row, "No questionnaires.");
            }
            else
            {
                var first = headerRow + 1;
                var titles = list.Questionnaires.Select(q => q.Title).ToList();
                var height = Math.Max(16, titles.Count * 2 + 8);
                var top = row + 1; // 0-based row index of the line after the table, plus a gap

                charts.Add(new ChartSpec(sheetName, "Average rating (out of 5)", ChartKind.Bar,
                    ExcelChartWriter.Ref(sheetName, 1, first, row), titles,
                    ExcelChartWriter.Ref(sheetName, 7, first, row), list.Questionnaires.Select(q => q.AverageRating).ToList(),
                    "Average", AverageColor, AverageFormat, 0, 5, 0, top, 3, top + height));

                charts.Add(new ChartSpec(sheetName, "Satisfied (Good or Very good)", ChartKind.Bar,
                    ExcelChartWriter.Ref(sheetName, 1, first, row), titles,
                    ExcelChartWriter.Ref(sheetName, 8, first, row), list.Questionnaires.Select(q => Fraction(q.SatisfactionPercent)).ToList(),
                    "Satisfied", SatisfiedColor, PercentFormat, 0, 1, 3, top, 9, top + height));
            }

            sheet.Column(1).Width = 36;
            sheet.Column(2).Width = 20;
            sheet.Column(3).Width = 24;
            sheet.Columns(4, headers.Length).Width = 14;
            sheet.Column(headers.Length).Width = 18;

            return ExcelChartWriter.AddCharts(Save(workbook), charts);
        }

        // ---------- per-questionnaire sheets ----------

        private static void WriteSummary(IXLWorksheet sheet, QuestionnaireExportData data)
        {
            var stats = data.Stats;
            WriteTitle(sheet, $"Al Amal Hospital - {stats.Title}", stats.From, stats.To);
            sheet.Cell(3, 1).Value = $"Department: {stats.DepartmentName ?? "-"}   ·   Link: /{stats.Slug}   ·   {(stats.IsActive ? "Active" : "Inactive")}";
            sheet.Cell(3, 1).Style.Font.Italic = true;

            var row = 5;
            sheet.Cell(row, 1).Value = "Responses";
            sheet.Cell(row++, 2).Value = stats.SubmissionCount;
            sheet.Cell(row, 1).Value = "Average rating (out of 5)";
            WriteAverage(sheet.Cell(row++, 2), stats.AverageRating);
            sheet.Cell(row, 1).Value = "Satisfied (Good or Very good)";
            WritePercent(sheet.Cell(row++, 2), stats.SatisfactionPercent);
            sheet.Range(5, 1, row - 1, 1).Style.Font.Bold = true;

            row++;
            sheet.Cell(row, 1).Value = "All answers";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            var total = Total(stats.Distribution);
            foreach (var (rating, label) in Ratings)
            {
                var count = CountOf(stats.Distribution, rating);
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 2).Value = count;
                sheet.Cell(row, 3).Value = total == 0 ? 0 : (double)count / total;
                sheet.Cell(row, 3).Style.NumberFormat.Format = PercentFormat;
                row++;
            }

            row++;
            sheet.Cell(row, 1).Value = "Charts are on the By Question and Trend sheets.";
            sheet.Cell(row, 1).Style.Font.Italic = true;
            sheet.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;

            Center(sheet.Range(5, 2, row - 1, 3));
            sheet.Column(1).Width = 34;
            sheet.Column(2).Width = 14;
            sheet.Column(3).Width = 10;
        }

        private static void WriteQuestions(IXLWorksheet sheet, QuestionnaireStatsResponse stats, List<ChartSpec> charts)
        {
            const string sheetName = "By Question";
            WriteTitle(sheet, "By question", stats.From, stats.To);

            var headers = new List<string> { "#", "Question", "Answers", "Average", "Satisfied" };
            foreach (var (_, label) in Ratings) headers.Add(label);
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);

            var row = headerRow;
            var number = 0;
            var labels = new List<string>();
            foreach (var q in stats.Questions)
            {
                row++;
                var label = q.IsArchived ? $"{q.Text} (removed from the page)" : q.Text;
                labels.Add(label);
                sheet.Cell(row, 1).Value = q.IsArchived ? "-" : (++number).ToString();
                sheet.Cell(row, 2).Value = label;
                sheet.Cell(row, 3).Value = q.AnswerCount;
                WriteAverage(sheet.Cell(row, 4), q.AverageRating);
                WritePercent(sheet.Cell(row, 5), q.SatisfactionPercent);
                var col = 6;
                foreach (var (rating, _) in Ratings)
                    sheet.Cell(row, col++).Value = CountOf(q.Distribution, rating);

                CenterRow(sheet, row, headers.Count);
                // Question text reads better aligned to its own language.
                sheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                sheet.Cell(row, 2).Style.Alignment.WrapText = true;
            }

            if (row == headerRow)
            {
                WriteNoData(sheet, ref row, "No questions.");
            }
            else
            {
                var first = headerRow + 1;
                var height = Math.Max(16, labels.Count * 3 + 6);
                var top = row + 1;

                // Stacked, same width (columns B..G), so the two read as a pair.
                charts.Add(new ChartSpec(sheetName, "Average rating per question (out of 5)", ChartKind.Bar,
                    ExcelChartWriter.Ref(sheetName, 2, first, row), labels,
                    ExcelChartWriter.Ref(sheetName, 4, first, row), stats.Questions.Select(q => q.AverageRating).ToList(),
                    "Average", AverageColor, AverageFormat, 0, 5, 1, top, 7, top + height));

                charts.Add(new ChartSpec(sheetName, "Satisfied per question (Good or Very good)", ChartKind.Bar,
                    ExcelChartWriter.Ref(sheetName, 2, first, row), labels,
                    ExcelChartWriter.Ref(sheetName, 5, first, row), stats.Questions.Select(q => Fraction(q.SatisfactionPercent)).ToList(),
                    "Satisfied", SatisfiedColor, PercentFormat, 0, 1, 1, top + height + 1, 7, top + height * 2 + 1));
            }

            sheet.Column(1).Width = 5;
            sheet.Column(2).Width = 50;
            sheet.Columns(3, headers.Count).Width = 12;
        }

        private static void WriteTrend(IXLWorksheet sheet, QuestionnaireTrend trend, List<ChartSpec> charts)
        {
            const string sheetName = "Trend";
            sheet.Cell(1, 1).Value = "Month by month";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(2, 1).Value = $"{trend.ReportMonth.Label} against every month before it. Blank = nobody answered that month.";
            sheet.Cell(2, 1).Style.Font.Italic = true;

            string[] headers = ["Month", "Responses", "Average", "Satisfied"];
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);

            var row = headerRow;
            foreach (var m in trend.Months)
            {
                row++;
                sheet.Cell(row, 1).Value = m.Label;
                sheet.Cell(row, 2).Value = m.Submissions;
                WriteAverage(sheet.Cell(row, 3), m.AverageRating);
                WritePercent(sheet.Cell(row, 4), m.SatisfactionPercent);
                CenterRow(sheet, row, headers.Length);
            }
            var lastMonthRow = row;
            // The report month stands out in the table the way it does in the email.
            sheet.Range(lastMonthRow, 1, lastMonthRow, headers.Length).Style.Font.Bold = true;

            row += 2;
            sheet.Cell(row, 1).Value = "This month vs before";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;
            var compareHeader = row;
            WriteHeader(sheet, compareHeader, ["Period", "Responses", "Average", "Satisfied"]);
            foreach (var p in new[] { trend.PreviousMonths, trend.ReportMonth })
            {
                row++;
                sheet.Cell(row, 1).Value = p.Label;
                sheet.Cell(row, 2).Value = p.Submissions;
                WriteAverage(sheet.Cell(row, 3), p.AverageRating);
                WritePercent(sheet.Cell(row, 4), p.SatisfactionPercent);
                CenterRow(sheet, row, headers.Length);
            }

            var avgDelta = trend.ReportMonth.AverageRating - trend.PreviousMonths.AverageRating;
            var satDelta = trend.ReportMonth.SatisfactionPercent - trend.PreviousMonths.SatisfactionPercent;
            row++;
            sheet.Cell(row, 1).Value = "Change";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            if (avgDelta is double a) { sheet.Cell(row, 3).Value = Math.Round(a, 2); sheet.Cell(row, 3).Style.NumberFormat.Format = "+0.00;-0.00;0.00"; }
            if (satDelta is double s) { sheet.Cell(row, 4).Value = Math.Round(s / 100, 3); sheet.Cell(row, 4).Style.NumberFormat.Format = "+0.0%;-0.0%;0.0%"; }
            CenterRow(sheet, row, headers.Length);

            sheet.Column(1).Width = 22;
            sheet.Columns(2, 4).Width = 13;

            // Charts to the right of the tables: the monthly lines on the left
            // column of charts, the "this month vs before" pair beside them.
            var firstMonth = headerRow + 1;
            var months = trend.Months.Select(m => m.Label).ToList();
            var compareFirst = compareHeader + 1;
            var compareLast = compareHeader + 2;
            var compareLabels = new[] { trend.PreviousMonths.Label, trend.ReportMonth.Label };

            charts.Add(new ChartSpec(sheetName, "Average rating by month (out of 5)", ChartKind.Line,
                ExcelChartWriter.Ref(sheetName, 1, firstMonth, lastMonthRow), months,
                ExcelChartWriter.Ref(sheetName, 3, firstMonth, lastMonthRow), trend.Months.Select(m => m.AverageRating).ToList(),
                "Average", AverageColor, AverageFormat, 1, 5, 5, 3, 14, 20));

            charts.Add(new ChartSpec(sheetName, "Satisfied by month", ChartKind.Column,
                ExcelChartWriter.Ref(sheetName, 1, firstMonth, lastMonthRow), months,
                ExcelChartWriter.Ref(sheetName, 4, firstMonth, lastMonthRow), trend.Months.Select(m => Fraction(m.SatisfactionPercent)).ToList(),
                "Satisfied", SatisfiedColor, PercentFormat, 0, 1, 5, 21, 14, 38));

            charts.Add(new ChartSpec(sheetName, "Responses by month", ChartKind.Column,
                ExcelChartWriter.Ref(sheetName, 1, firstMonth, lastMonthRow), months,
                ExcelChartWriter.Ref(sheetName, 2, firstMonth, lastMonthRow), trend.Months.Select(m => (double?)m.Submissions).ToList(),
                "Responses", ResponsesColor, "0", 0, null, 5, 39, 14, 56));

            charts.Add(new ChartSpec(sheetName, "Average: this month vs before", ChartKind.Column,
                ExcelChartWriter.Ref(sheetName, 1, compareFirst, compareLast), compareLabels,
                ExcelChartWriter.Ref(sheetName, 3, compareFirst, compareLast), [trend.PreviousMonths.AverageRating, trend.ReportMonth.AverageRating],
                "Average", AverageColor, AverageFormat, 0, 5, 15, 3, 21, 20));

            charts.Add(new ChartSpec(sheetName, "Satisfied: this month vs before", ChartKind.Column,
                ExcelChartWriter.Ref(sheetName, 1, compareFirst, compareLast), compareLabels,
                ExcelChartWriter.Ref(sheetName, 4, compareFirst, compareLast), [Fraction(trend.PreviousMonths.SatisfactionPercent), Fraction(trend.ReportMonth.SatisfactionPercent)],
                "Satisfied", SatisfiedColor, PercentFormat, 0, 1, 15, 21, 21, 38));
        }

        private static void WriteResponses(IXLWorksheet sheet, QuestionnaireExportData data)
        {
            var stats = data.Stats;
            WriteTitle(sheet, "Responses", stats.From, stats.To);
            if (data.Truncated)
            {
                sheet.Cell(3, 1).Value = $"Only the newest {data.ExportCap:N0} responses are included.";
                sheet.Cell(3, 1).Style.Font.FontColor = XLColor.DarkRed;
            }

            // Every question that has a column: live ones, and removed ones that
            // still hold answers in this period — the same list the second sheet shows.
            var headers = new List<string> { "Date", "Name", "Phone", "Average" };
            foreach (var q in stats.Questions) headers.Add(q.IsArchived ? $"{q.Text} (removed)" : q.Text);
            headers.Add("Notes");
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);
            sheet.Row(headerRow).Style.Alignment.WrapText = true;

            var notesCol = headers.Count;
            var row = headerRow;
            foreach (var r in data.Responses)
            {
                row++;
                sheet.Cell(row, 1).Value = r.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                sheet.Cell(row, 2).Value = r.Name ?? "-";
                // Text, not a number: Excel would drop the leading zero.
                sheet.Cell(row, 3).Value = r.PhoneNumber ?? "-";
                sheet.Cell(row, 3).Style.NumberFormat.Format = "@";
                WriteAverage(sheet.Cell(row, 4), r.AverageRating);

                var col = 5;
                foreach (var q in stats.Questions)
                {
                    sheet.Cell(row, col++).Value = r.Ratings.TryGetValue(q.Id, out var rating) ? LabelOf(rating) : "-";
                }

                sheet.Cell(row, notesCol).Value = r.Notes ?? "";
                CenterRow(sheet, row, notesCol - 1);
                sheet.Cell(row, notesCol).Style.Alignment.WrapText = true;
                sheet.Cell(row, notesCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                sheet.Cell(row, notesCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            if (row == headerRow) WriteNoData(sheet, ref row, "No responses for this period.");
            else sheet.SheetView.FreezeRows(headerRow);

            sheet.Column(1).Width = 17;
            sheet.Column(2).Width = 22;
            sheet.Column(3).Width = 16;
            sheet.Column(4).Width = 10;
            for (var c = 5; c < notesCol; c++) sheet.Column(c).Width = 18;
            sheet.Column(notesCol).Width = 50;
        }

        // ---------- helpers ----------

        private static void WriteTitle(IXLWorksheet sheet, string title, DateOnly? from, DateOnly? to)
        {
            sheet.Cell(1, 1).Value = title;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            var period = from is null && to is null ? "All time" : $"{FormatDate(from)} - {FormatDate(to)}";
            sheet.Cell(2, 1).Value = $"Report period: {period}";
            sheet.Cell(2, 1).Style.Font.Italic = true;
        }

        private static void WriteHeader(IXLWorksheet sheet, int headerRow, IReadOnlyList<string> headers)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = HeaderFill;
                Center(cell);
            }
        }

        private static void CenterRow(IXLWorksheet sheet, int row, int columns) =>
            Center(sheet.Range(row, 1, row, columns));

        private static void Center(IXLRange range)
        {
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void Center(IXLCell cell)
        {
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        private static void WriteNoData(IXLWorksheet sheet, ref int row, string message)
        {
            row++;
            sheet.Cell(row, 1).Value = message;
            sheet.Cell(row, 1).Style.Font.Italic = true;
        }

        // Real numbers, so the charts can read the cells. Null stays blank —
        // "nothing answered" is a gap on a chart, not a zero.
        private static void WriteAverage(IXLCell cell, double? value)
        {
            if (value is null) return;
            cell.Value = Math.Round(value.Value, 2);
            cell.Style.NumberFormat.Format = AverageFormat;
        }

        private static void WritePercent(IXLCell cell, double? percent)
        {
            if (percent is null) return;
            cell.Value = Math.Round(percent.Value / 100, 3);
            cell.Style.NumberFormat.Format = PercentFormat;
        }

        private static double? Fraction(double? percent) => percent is null ? null : Math.Round(percent.Value / 100, 3);

        private static int CountOf(RatingDistributionResponse d, QuestionRating rating) => rating switch
        {
            QuestionRating.VeryGood => d.VeryGood,
            QuestionRating.Good => d.Good,
            QuestionRating.Mid => d.Mid,
            QuestionRating.Bad => d.Bad,
            _ => d.VeryBad
        };

        private static int Total(RatingDistributionResponse d) => d.VeryGood + d.Good + d.Mid + d.Bad + d.VeryBad;

        private static string LabelOf(QuestionRating rating) => Array.Find(Ratings, r => r.Rating == rating).Label;

        private static string FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? "All time";

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
