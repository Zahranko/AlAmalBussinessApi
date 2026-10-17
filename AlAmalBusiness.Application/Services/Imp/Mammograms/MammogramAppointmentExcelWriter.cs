using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlAmalBusiness.Application.Services.Imp.Mammograms
{
    // The appointments booked in a range, one row each, in the layout of the
    // old app's export: the fixed columns, then three per follow-up (outcome,
    // note, date) for as many follow-ups as the busiest appointment had.
    internal static class MammogramAppointmentExcelWriter
    {
        private static readonly string[] Headers =
        {
            "#", "Appointment #", "Name", "Phone", "Status", "Procedure", "Room",
            "Appointment Date", "Minutes", "Create Date", "Booked by",
            "Referring doctor", "Receiving doctor", "Description"
        };

        public static byte[] Build(List<MammogramAppointmentRow> items, List<MammogramAppointmentHistoryRow> followUps)
        {
            var byAppointment = followUps.GroupBy(f => f.AppointmentId).ToDictionary(g => g.Key, g => g.ToList());
            var maxFollowUps = byAppointment.Count == 0 ? 0 : byAppointment.Values.Max(l => l.Count);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Appointments");

            var headers = Headers.ToList();
            for (var i = 1; i <= maxFollowUps; i++)
                headers.AddRange(new[] { $"Follow Up {i}", $"Follow Up {i} notes", $"Follow Up {i} date" });

            for (var c = 0; c < headers.Count; c++)
            {
                var cell = sheet.Cell(1, c + 1);
                cell.Value = headers[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2FF");
            }

            var r = 2;
            foreach (var a in items)
            {
                sheet.Cell(r, 1).Value = r - 1;
                sheet.Cell(r, 2).Value = a.Id;
                sheet.Cell(r, 3).Value = a.FullName;
                // Text, so the leading zero stays.
                sheet.Cell(r, 4).Value = a.PhoneCountryCode == "+962" ? "0" + a.PhoneNumber : $"{a.PhoneCountryCode} {a.PhoneNumber}";
                sheet.Cell(r, 5).Value = StatusText(a.Status);
                sheet.Cell(r, 6).Value = MammogramExcelWriter.ProcedureText(a.Procedures);
                sheet.Cell(r, 7).Value = a.Room;
                SetDateTime(sheet.Cell(r, 8), a.AppointmentAt);
                sheet.Cell(r, 9).Value = a.DurationMinutes;
                SetDateTime(sheet.Cell(r, 10), a.CreatedAt);
                sheet.Cell(r, 11).Value = a.CreatedByName;
                sheet.Cell(r, 12).Value = a.ReferringDoctor;
                sheet.Cell(r, 13).Value = a.ReceivingDoctor;
                sheet.Cell(r, 14).Value = a.Notes;

                if (byAppointment.TryGetValue(a.Id, out var list))
                {
                    var c = Headers.Length + 1;
                    foreach (var f in list)
                    {
                        sheet.Cell(r, c).Value = FollowUpText(f);
                        sheet.Cell(r, c + 1).Value = f.Note;
                        // A follow-up that moved the slot is dated by the new
                        // slot, as the old export did; the rest by when.
                        SetDateTime(sheet.Cell(r, c + 2), f.AppointmentAt ?? f.CreatedAt);
                        c += 3;
                    }
                }
                r++;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, Math.Max(r - 1, 1), headers.Count).SetAutoFilter();
            sheet.Columns().AdjustToContents();
            foreach (var col in sheet.ColumnsUsed()) if (col.Width > 60) col.Width = 60;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private static void SetDateTime(IXLCell cell, DateTime value)
        {
            cell.Value = value;
            cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        }

        private static string StatusText(MammogramAppointmentStatus status) => status switch
        {
            MammogramAppointmentStatus.Booked => "Booked",
            MammogramAppointmentStatus.FollowUp => "Follow Up",
            MammogramAppointmentStatus.Arrived => "Arrived",
            MammogramAppointmentStatus.Success => "Success",
            MammogramAppointmentStatus.Failed => "Failed",
            _ => status.ToString()
        };

        private static string FollowUpText(MammogramAppointmentHistoryRow f)
        {
            var text = f.ResultingStatus is { } s ? StatusText(s) : "";
            return f.CallSucceeded switch
            {
                true => $"{text} (reached)",
                false => $"{text} (no answer)",
                _ => text
            };
        }
    }
}
