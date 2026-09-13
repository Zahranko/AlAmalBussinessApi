using AlAmalBusiness.Application.DTOs.Questionnaires.Response;
using AlAmalBusiness.Application.Services.Interface.Questionnaires;
using AlAmalBusiness.Domain.Constants;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;

namespace AlAmalBusiness.Application.Services.Imp.Questionnaires
{
    // Questionnaire results as workbooks, in the feedback report's style
    // (FeedbackExcelReportService): a bold title, an italic period line, a
    // tinted header row, centred values.
    //
    // Scoping is never decided here — QuestionnaireService has already pinned
    // a QManager to their own department before any of this runs.
    public class QuestionnaireExcelReportService : IQuestionnaireExcelReportService
    {
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#EEF2FF");

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
            using var workbook = new XLWorkbook();

            WriteSummary(workbook.Worksheets.Add("Summary"), data.Stats);
            WriteQuestions(workbook.Worksheets.Add("By Question"), data.Stats);
            WriteResponses(workbook.Worksheets.Add("Responses"), data);

            return Save(workbook);
        }

        public byte[] BuildOverview(QuestionnaireListResponse list)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Questionnaires");

            WriteTitle(sheet, "Al Amal Hospital - Patient Questionnaires", list.From, list.To);

            var row = 4;
            foreach (var (label, value) in new (string, string)[]
            {
                ("Questionnaires", $"{list.QuestionnaireCount} ({list.ActiveCount} active)"),
                ("Responses", list.TotalSubmissions.ToString()),
                ("Average rating (out of 5)", Average(list.AverageRating)),
                ("Satisfied (Good or Very good)", PercentText(list.SatisfactionPercent))
            })
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 1).Style.Font.Bold = true;
                sheet.Cell(row, 2).Value = value;
                Center(sheet.Cell(row, 2));
                row++;
            }

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
                sheet.Cell(row, 8).Value = PercentText(q.SatisfactionPercent);
                sheet.Cell(row, 9).Value = q.LastSubmissionDate?.ToString("yyyy-MM-dd HH:mm") ?? "-";
                CenterRow(sheet, row, headers.Length);
            }

            if (row == headerRow) WriteNoData(sheet, ref row, "No questionnaires.");

            sheet.Column(1).Width = 36;
            sheet.Column(2).Width = 20;
            sheet.Column(3).Width = 24;
            sheet.Columns(4, headers.Length).Width = 14;
            sheet.Column(headers.Length).Width = 18;

            return Save(workbook);
        }

        // ---------- per-questionnaire sheets ----------

        private static void WriteSummary(IXLWorksheet sheet, QuestionnaireStatsResponse stats)
        {
            WriteTitle(sheet, $"Al Amal Hospital - {stats.Title}", stats.From, stats.To);
            sheet.Cell(3, 1).Value = $"Department: {stats.DepartmentName ?? "-"}   ·   Link: /{stats.Slug}   ·   {(stats.IsActive ? "Active" : "Inactive")}";
            sheet.Cell(3, 1).Style.Font.Italic = true;

            var row = 5;
            foreach (var (label, value) in new (string, string)[]
            {
                ("Responses", stats.SubmissionCount.ToString()),
                ("Average rating (out of 5)", Average(stats.AverageRating)),
                ("Satisfied (Good or Very good)", PercentText(stats.SatisfactionPercent))
            })
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 2).Value = value;
                row++;
            }
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
                sheet.Cell(row, 3).Value = $"{Percent(count, total)}%";
                row++;
            }

            Center(sheet.Range(5, 2, row, 3));
            sheet.Column(1).Width = 34;
            sheet.Column(2).Width = 14;
            sheet.Column(3).Width = 10;
        }

        private static void WriteQuestions(IXLWorksheet sheet, QuestionnaireStatsResponse stats)
        {
            WriteTitle(sheet, "By question", stats.From, stats.To);

            var headers = new List<string> { "#", "Question", "Answers", "Average", "Satisfied" };
            foreach (var (_, label) in Ratings) headers.Add(label);
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);

            var row = headerRow;
            var number = 0;
            foreach (var q in stats.Questions)
            {
                row++;
                sheet.Cell(row, 1).Value = q.IsArchived ? "-" : (++number).ToString();
                sheet.Cell(row, 2).Value = q.IsArchived ? $"{q.Text} (removed from the page)" : q.Text;
                sheet.Cell(row, 3).Value = q.AnswerCount;
                WriteAverage(sheet.Cell(row, 4), q.AverageRating);
                sheet.Cell(row, 5).Value = PercentText(q.SatisfactionPercent);
                var col = 6;
                foreach (var (rating, _) in Ratings)
                    sheet.Cell(row, col++).Value = CountOf(q.Distribution, rating);

                CenterRow(sheet, row, headers.Count);
                // Question text reads better aligned to its own language.
                sheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                sheet.Cell(row, 2).Style.Alignment.WrapText = true;
            }

            if (row == headerRow) WriteNoData(sheet, ref row, "No questions.");

            sheet.Column(1).Width = 5;
            sheet.Column(2).Width = 50;
            sheet.Columns(3, headers.Count).Width = 12;
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

        // Null is not zero: "nothing answered" reads as a dash, the same
        // distinction the screen draws.
        private static void WriteAverage(IXLCell cell, double? value)
        {
            if (value is null) cell.Value = "-";
            else cell.Value = Math.Round(value.Value, 2);
        }

        private static string Average(double? value) => value is null ? "-" : $"{value.Value:0.00}";

        private static string PercentText(double? value) => value is null ? "-" : $"{value}%";

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

        private static double Percent(int part, int total) =>
            total == 0 ? 0 : Math.Round(part * 100d / total, 1);

        private static string FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? "All time";

        private static byte[] Save(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
