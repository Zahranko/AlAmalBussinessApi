using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.Mammograms;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.Mammograms
{
    // One month's sheet as an .xlsx with the old workbook's exact headers and
    // column order (its 9-2026 tab), so anyone still working from the Excel
    // file reads it the same way.
    internal static class MammogramExcelWriter
    {
        private static readonly string[] Headers =
        {
            "ID", "Female/male", "Name", "Tech", "THE AGE", "Breast feeding mother",
            "احتمالية حدوث حمل", "implants", "Vis_Date", "Procedure", "Screening/ Diadnostic",
            "B", "Pros", "Follow Up-Date", "Call Follow Up", "التواصل", "وقت التواصل"
        };

        public static byte[] Build(string title, List<MammogramRecord> rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(title);

            for (var c = 0; c < Headers.Length; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = Headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            var r = 2;
            foreach (var row in rows)
            {
                sheet.Cell(r, 1).Value = row.PatientMrn;
                sheet.Cell(r, 2).Value = row.Gender == Gender.Male ? "M" : "F";
                sheet.Cell(r, 3).Value = row.FullName;
                sheet.Cell(r, 4).Value = row.Tech;
                if (row.Age is { } age) sheet.Cell(r, 5).Value = age;
                sheet.Cell(r, 6).Value = row.BreastFeeding ? "YES" : "";
                sheet.Cell(r, 7).Value = row.PossiblePregnancy ? "YES" : "";
                sheet.Cell(r, 8).Value = row.HasImplants ? "YES" : "";
                SetDate(sheet.Cell(r, 9), row.VisitDate.ToDateTime(TimeOnly.MinValue));
                sheet.Cell(r, 10).Value = ProcedureText(row.Procedures);
                sheet.Cell(r, 11).Value = row.ExamType switch
                {
                    MammogramExamType.Screening => "S",
                    MammogramExamType.Diagnostic => "D",
                    _ => ""
                };
                sheet.Cell(r, 12).Value = row.BiRads;
                sheet.Cell(r, 13).Value = row.Recommendation;
                if (row.FollowUpDate is { } followUp) SetDate(sheet.Cell(r, 14), followUp.ToDateTime(TimeOnly.MinValue));
                // Text, keeping the leading zero the old sheet lost to Excel's
                // number formatting.
                sheet.Cell(r, 15).Value = PhoneText(row.PhoneCountryCode, row.PhoneNumber);
                sheet.Cell(r, 16).Value = row.ContactNote;
                if (row.ContactedAt is { } contacted)
                {
                    sheet.Cell(r, 17).Value = contacted;
                    sheet.Cell(r, 17).Style.DateFormat.Format = "d-m-yyyy h:mm";
                }
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, Math.Max(r - 1, 1), Headers.Length).SetAutoFilter();
            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetDate(IXLCell cell, DateTime value)
        {
            cell.Value = value;
            cell.Style.DateFormat.Format = "d/m/yyyy";
        }

        // The sheet's own spelling: "MG+B.U.S+BREAST MRI+BIOPSY".
        internal static string ProcedureText(MammogramProcedures p) =>
            string.Join("+", new (MammogramProcedures Flag, string Label)[]
            {
                (MammogramProcedures.Mammogram, "MG"),
                (MammogramProcedures.BreastUltrasound, "B.U.S"),
                (MammogramProcedures.BreastMri, "BREAST MRI"),
                (MammogramProcedures.Biopsy, "BIOPSY")
            }.Where(x => p.HasFlag(x.Flag)).Select(x => x.Label));

        private static string PhoneText(string countryCode, string? number) =>
            string.IsNullOrEmpty(number) ? "" : countryCode == "+962" ? "0" + number : $"{countryCode} {number}";
    }
}
