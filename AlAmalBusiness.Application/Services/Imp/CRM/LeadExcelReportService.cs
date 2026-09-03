using AlAmalBusiness.Application.DTOs.CRM.Stats;
using AlAmalBusiness.Application.Services.Interface.CRM;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.CRM
{
    public class LeadExcelReportService : ILeadExcelReportService
    {
        public byte[] Build(HospitalManagerStatsDTO stats)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Hospital Report");

            sheet.Cell(1, 1).Value = "Al Amal Hospital - Report";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            var period = stats.From is null && stats.To is null
                ? "All time"
                : $"{FormatDate(stats.From)} - {FormatDate(stats.To)}";
            sheet.Cell(2, 1).Value = $"Report period: {period}";
            sheet.Cell(2, 1).Style.Font.Italic = true;

            sheet.Cell(4, 1).Value = "Total Leads";
            sheet.Cell(4, 2).Value = stats.TotalLeads;
            sheet.Cell(5, 1).Value = "Pending";
            sheet.Cell(5, 2).Value = stats.PendingCount;
            sheet.Cell(5, 3).Value = $"{stats.PendingPercent}%";
            sheet.Cell(6, 1).Value = "Waiting";
            sheet.Cell(6, 2).Value = stats.WaitingCount;
            sheet.Cell(6, 3).Value = $"{stats.WaitingPercent}%";
            sheet.Cell(7, 1).Value = "Success";
            sheet.Cell(7, 2).Value = stats.SuccessCount;
            sheet.Cell(7, 3).Value = $"{stats.SuccessPercent}%";
            sheet.Cell(8, 1).Value = "Closed";
            sheet.Cell(8, 2).Value = stats.ClosedCount;
            sheet.Cell(8, 3).Value = $"{stats.ClosedPercent}%";
            sheet.Range(4, 1, 8, 1).Style.Font.Bold = true;

            var doctorRow = WriteDoctorTable(sheet, startRow: 10, title: "By Doctor", stats.Doctors);

            WriteTable(sheet, startRow: doctorRow + 2, title: "By Procedure", rows: stats.Procedures
                .Select(p => (p.Name, p.TotalLeads, p.PendingCount, p.WaitingCount, p.SuccessCount, p.ClosedCount, p.SuccessRate)));

            sheet.Column(1).Width = 30;
            sheet.Column(8).Width = 45;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static int WriteDoctorTable(IXLWorksheet sheet, int startRow, string title, List<GroupStatDTO> doctors)
        {
            sheet.Cell(startRow, 1).Value = title;
            sheet.Cell(startRow, 1).Style.Font.Bold = true;
            sheet.Cell(startRow, 1).Style.Font.FontSize = 12;

            var headerRow = startRow + 1;
            string[] headers = ["Name", "Total Leads", "Pending", "Waiting", "Success", "Closed", "Success Rate", "Procedures Breakdown"];

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
                if (i < 7)
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            var row = headerRow;
            foreach (var d in doctors)
            {
                row++;
                sheet.Cell(row, 1).Value = d.Name;
                sheet.Cell(row, 2).Value = d.TotalLeads;
                sheet.Cell(row, 3).Value = d.PendingCount;
                sheet.Cell(row, 4).Value = d.WaitingCount;
                sheet.Cell(row, 5).Value = d.SuccessCount;
                sheet.Cell(row, 6).Value = d.ClosedCount;
                sheet.Cell(row, 7).Value = $"{d.SuccessRate}%";
                for (var i = 1; i <= 7; i++)
                {
                    sheet.Cell(row, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    sheet.Cell(row, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                var activeProcedures = d.Procedures
                    .Where(p => p.Count > 0)
                    .Select(p => $"{p.ProcedureName} = {p.Count}")
                    .ToList();

                var procCell = sheet.Cell(row, 8);
                procCell.Value = activeProcedures.Count > 0 ? string.Join("\n", activeProcedures) : "-";
                procCell.Style.Alignment.WrapText = true;
                procCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            if (row > headerRow)
                sheet.Rows(headerRow + 1, row).AdjustToContents(minHeight: 15, maxHeight: 150);

            if (row == headerRow)
            {
                row++;
                sheet.Cell(row, 1).Value = "No data for this period.";
                sheet.Cell(row, 1).Style.Font.Italic = true;
            }

            return row;
        }

        private static int WriteTable(IXLWorksheet sheet, int startRow, string title, IEnumerable<(string Name, int Total, int Pending, int Waiting, int Success, int Closed, double SuccessRate)> rows)
        {
            sheet.Cell(startRow, 1).Value = title;
            sheet.Cell(startRow, 1).Style.Font.Bold = true;
            sheet.Cell(startRow, 1).Style.Font.FontSize = 12;

            var headerRow = startRow + 1;
            string[] headers = ["Name", "Total Leads", "Pending", "Waiting", "Success", "Closed", "Success Rate"];
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            var row = headerRow;
            foreach (var r in rows)
            {
                row++;
                sheet.Cell(row, 1).Value = r.Name;
                sheet.Cell(row, 2).Value = r.Total;
                sheet.Cell(row, 3).Value = r.Pending;
                sheet.Cell(row, 4).Value = r.Waiting;
                sheet.Cell(row, 5).Value = r.Success;
                sheet.Cell(row, 6).Value = r.Closed;
                sheet.Cell(row, 7).Value = $"{r.SuccessRate}%";
                for (var i = 1; i <= 7; i++)
                {
                    sheet.Cell(row, i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    sheet.Cell(row, i).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
            }

            if (row == headerRow)
            {
                row++;
                sheet.Cell(row, 1).Value = "No data for this period.";
                sheet.Cell(row, 1).Style.Font.Italic = true;
            }

            return row;
        }

        private static string FormatDate(DateTime? date) => date?.ToString("yyyy-MM-dd") ?? "All time";

        public byte[] BuildDoctorLeads(DoctorLeadExportDTO export)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(TrimSheetName(export.DoctorName));

            sheet.Cell(1, 1).Value = $"Al Amal Hospital - Leads for Dr. {export.DoctorName}";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 14;

            string[] headers = ["Patient Name", "Status", "Procedure", "Referral Source", "Created By", "Claimed By", "Created Date", "Follow-up Notes"];
            const int headerRow = 3;
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            var row = headerRow;
            foreach (var l in export.Leads)
            {
                row++;
                sheet.Cell(row, 1).Value = l.PatientName;
                sheet.Cell(row, 2).Value = l.Status;
                sheet.Cell(row, 3).Value = l.Procedure ?? "";
                sheet.Cell(row, 4).Value = l.ReferralSource ?? "";
                sheet.Cell(row, 5).Value = l.CreatedByName ?? "";
                sheet.Cell(row, 6).Value = l.ClaimedByName ?? "";
                sheet.Cell(row, 7).Value = l.CreatedDate?.ToString("yyyy-MM-dd") ?? "";
                var notesCell = sheet.Cell(row, 8);
                notesCell.Value = l.FollowUpNotes.Count > 0 ? string.Join("\n", l.FollowUpNotes) : "";
                notesCell.Style.Alignment.WrapText = true;
            }

            if (row == headerRow)
            {
                row++;
                sheet.Cell(row, 1).Value = "No leads for this period.";
                sheet.Cell(row, 1).Style.Font.Italic = true;
            }

            sheet.Columns(1, 7).AdjustToContents();
            sheet.Column(8).Width = 70;
            sheet.Row(headerRow).Height = 18;
            if (row > headerRow) sheet.Rows(headerRow + 1, row).AdjustToContents(minHeight: 15, maxHeight: 200);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static string TrimSheetName(string name)
        {
            var cleaned = new string(name.Where(ch => !"\\/?*[]".Contains(ch)).ToArray());
            return cleaned.Length > 31 ? cleaned[..31] : (cleaned.Length > 0 ? cleaned : "Leads");
        }
    }
}
