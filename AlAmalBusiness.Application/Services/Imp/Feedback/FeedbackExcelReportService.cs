using AlAmalBusiness.Application.DTOs.Feedback.Response;
using AlAmalBusiness.Application.Services.Interface.Feedback;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.Feedback
{
    // The feedback dashboard as a workbook, laid out the way the screen reads
    // it: the period's status split first, then the per-department breakdown,
    // then who closed what.
    //
    // Scoping is not decided here. GetStatsAsync has already pinned a manager
    // to their own department, so their Departments list holds exactly their
    // one row, and the same code path hands an admin every row.
    public class FeedbackExcelReportService : IFeedbackExcelReportService
    {
        private static readonly XLColor HeaderFill = XLColor.FromHtml("#EEF2FF");

        public byte[] Build(FeedbackStatsResponse stats)
        {
            using var workbook = new XLWorkbook();

            WriteSummary(workbook.Worksheets.Add("Summary"), stats);
            WriteDepartments(workbook.Worksheets.Add("By Department"), stats);
            WriteResolvers(workbook.Worksheets.Add("Resolved By"), stats);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void WriteSummary(IXLWorksheet sheet, FeedbackStatsResponse stats)
        {
            sheet.Cell(1, 1).Value = "Al Amal Hospital - Patient Feedback Report";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            var period = stats.From is null && stats.To is null
                ? "All time"
                : $"{FormatDate(stats.From)} - {FormatDate(stats.To)}";
            sheet.Cell(2, 1).Value = $"Report period: {period}";
            sheet.Cell(2, 1).Style.Font.Italic = true;

            sheet.Cell(3, 1).Value = $"Scope: {ScopeLabel(stats)}";
            sheet.Cell(3, 1).Style.Font.Italic = true;

            sheet.Cell(5, 1).Value = "Messages received";
            sheet.Cell(5, 2).Value = stats.Total;

            var row = 6;
            foreach (var (label, count) in new (string Label, int Count)[]
            {
                ("New", stats.NewCount),
                ("In review", stats.InReviewCount),
                ("Resolved", stats.ResolvedCount),
                ("Archived", stats.ArchivedCount)
            })
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 2).Value = count;
                sheet.Cell(row, 3).Value = $"{Percent(count, stats.Total)}%";
                row++;
            }

            sheet.Range(5, 1, row - 1, 1).Style.Font.Bold = true;

            row++;
            sheet.Cell(row, 1).Value = "By type";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            foreach (var (label, count) in new (string Label, int Count)[]
            {
                ("Complaint", stats.ComplaintCount),
                ("Suggestion", stats.SuggestionCount),
                ("Thanks", stats.ThanksCount)
            })
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 2).Value = count;
                sheet.Cell(row, 3).Value = $"{Percent(count, stats.Total)}%";
                row++;
            }

            row++;
            var totalsFrom = row;
            sheet.Cell(row, 1).Value = "Resolved share";
            sheet.Cell(row, 2).Value = $"{stats.ResolvedPercent}%";
            row++;
            sheet.Cell(row, 1).Value = "Average time to resolve (hours)";
            WriteHours(sheet.Cell(row, 2), stats.AvgResolutionHours);
            row++;
            sheet.Cell(row, 1).Value = "Oldest message still open (hours)";
            WriteHours(sheet.Cell(row, 2), stats.OldestOpenHours);

            sheet.Range(totalsFrom, 1, row, 1).Style.Font.Bold = true;
            sheet.Column(1).Width = 34;
            sheet.Column(2).Width = 14;
            sheet.Column(3).Width = 10;
        }

        private static void WriteDepartments(IXLWorksheet sheet, FeedbackStatsResponse stats)
        {
            sheet.Cell(1, 1).Value = "By department";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 12;
            sheet.Cell(2, 1).Value = ScopeLabel(stats);
            sheet.Cell(2, 1).Style.Font.Italic = true;

            string[] headers = ["Department", "Received", "Open", "Resolved", "Archived", "Resolved Rate", "Avg Resolve (hours)"];
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);

            var row = headerRow;
            foreach (var d in stats.Departments)
            {
                row++;
                sheet.Cell(row, 1).Value = d.Name ?? "-";
                sheet.Cell(row, 2).Value = d.Total;
                sheet.Cell(row, 3).Value = d.OpenCount;
                sheet.Cell(row, 4).Value = d.ResolvedCount;
                sheet.Cell(row, 5).Value = d.ArchivedCount;
                sheet.Cell(row, 6).Value = $"{d.ResolvedPercent}%";
                WriteHours(sheet.Cell(row, 7), d.AvgResolutionHours);
                CenterRow(sheet, row, headers.Length);
            }

            // An admin reading a period nothing landed in, and a manager whose
            // own department is empty, both end up here.
            if (row == headerRow) WriteNoData(sheet, ref row);

            // A total line only says something when there is more than one
            // department stacked up in it.
            if (stats.Departments.Count > 1)
            {
                row += 2;
                sheet.Cell(row, 1).Value = "All departments";
                sheet.Cell(row, 2).Value = stats.Departments.Sum(d => d.Total);
                sheet.Cell(row, 3).Value = stats.Departments.Sum(d => d.OpenCount);
                sheet.Cell(row, 4).Value = stats.Departments.Sum(d => d.ResolvedCount);
                sheet.Cell(row, 5).Value = stats.Departments.Sum(d => d.ArchivedCount);
                sheet.Cell(row, 6).Value = $"{stats.ResolvedPercent}%";
                WriteHours(sheet.Cell(row, 7), stats.AvgResolutionHours);
                sheet.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
                CenterRow(sheet, row, headers.Length);
            }

            sheet.Column(1).Width = 32;
            sheet.Columns(2, headers.Length).Width = 16;
        }

        private static void WriteResolvers(IXLWorksheet sheet, FeedbackStatsResponse stats)
        {
            sheet.Cell(1, 1).Value = "Resolved by";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 12;
            sheet.Cell(2, 1).Value = "Counted per resolution, not per message.";
            sheet.Cell(2, 1).Style.Font.Italic = true;

            string[] headers = ["Staff", "Resolved", "Avg Resolve (hours)"];
            const int headerRow = 4;
            WriteHeader(sheet, headerRow, headers);

            var row = headerRow;
            foreach (var r in stats.Resolvers)
            {
                row++;
                sheet.Cell(row, 1).Value = r.UserName ?? "-";
                sheet.Cell(row, 2).Value = r.ResolvedCount;
                WriteHours(sheet.Cell(row, 3), r.AvgResolutionHours);
                CenterRow(sheet, row, headers.Length);
            }

            if (row == headerRow) WriteNoData(sheet, ref row);

            sheet.Column(1).Width = 32;
            sheet.Columns(2, headers.Length).Width = 18;
        }

        private static void WriteHeader(IXLWorksheet sheet, int headerRow, IReadOnlyList<string> headers)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = HeaderFill;
                if (i > 0) cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        // The name column stays left-aligned; every number after it centres,
        // the same as the hospital report.
        private static void CenterRow(IXLWorksheet sheet, int row, int columns)
        {
            for (var i = 2; i <= columns; i++)
                sheet.Cell(row, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void WriteNoData(IXLWorksheet sheet, ref int row)
        {
            row++;
            sheet.Cell(row, 1).Value = "No data for this period.";
            sheet.Cell(row, 1).Style.Font.Italic = true;
        }

        // Null is not zero: nothing resolved yet reads as a dash, the same
        // distinction the dashboard draws.
        private static void WriteHours(IXLCell cell, double? hours)
        {
            if (hours is null)
            {
                cell.Value = "-";
                return;
            }

            cell.Value = Math.Round(hours.Value, 1);
        }

        // What the rows below actually cover. The department list is grouped
        // out of the messages in scope, so an empty list means nothing arrived
        // in the period — which is not the same as "every department", and a
        // manager reading their own quiet month must not be told it is.
        private static string ScopeLabel(FeedbackStatsResponse stats) => stats.Departments.Count switch
        {
            0 => "No messages in this period",
            1 => stats.Departments[0].Name ?? "-",
            _ => "All departments"
        };

        private static double Percent(int part, int total) =>
            total == 0 ? 0 : Math.Round(part * 100d / total, 1);

        private static string FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? "All time";
    }
}
