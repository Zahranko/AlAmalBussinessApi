using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.Mammograms;
using AlAmalBusiness.Application.Services.Interface.Mammograms;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using AlAmalBusiness.Domain.Models.Mammograms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Mammograms
{
    // The mammogram day calendar, ported from the alamal-appointments app's
    // Cases feature. The rules that came across:
    //
    // - A slot is a quarter-hour start inside business hours (08:00-17:00),
    //   in one of two rooms, for one of the fixed session lengths; two
    //   appointments may overlap in time only in different rooms.
    // - A follow-up is a call (optionally moving the slot and noting whether
    //   the patient was reached), Success, or Failed, which needs a reason.
    // - "Arrived" (the old Assign button) is offered once the appointment's
    //   day has come, not its minute — a patient booked for later today can
    //   walk in early.
    //
    // Deliberate changes from that app: anyone with the role works any
    // appointment (it had owner-only actions, which a team sharing one
    // calendar kept tripping over); a Failed appointment gives its slot back,
    // and every other status keeps it (it only counted Pending, so a slot
    // under follow-up could be double-booked); moving a slot during a
    // follow-up is checked for overlaps like any booking (it wasn't); and the
    // admin-managed duration/procedure/department lists became the fixed
    // values below and the register's own procedure flags.
    public class MammogramAppointmentService : IMammogramAppointmentService
    {
        public const int OpensAtHour = 8;
        public const int ClosesAtHour = 17;
        public const int RoomCount = 2;
        public static readonly int[] Durations = { 15, 30, 45, 60, 90, 120 };

        private const int MaxExportDays = 366;

        private readonly IMammogramAppointmentRepo _appointments;

        public MammogramAppointmentService(IMammogramAppointmentRepo appointments)
        {
            _appointments = appointments;
        }

        // ---------- reads ----------

        public async Task<PagedResultDTO<MammogramAppointmentResponse>> GetPagedAsync(MammogramAppointmentListQueryDTO dto, string userId)
        {
            var query = new MammogramAppointmentQuery
            {
                View = dto.View,
                Status = dto.Status,
                Search = dto.Search,
                Page = dto.Page,
                PageSize = dto.PageSize,
                UserId = userId,
                Today = AppClock.Today
            };
            var (items, total) = await _appointments.PageAsync(query);
            return new PagedResultDTO<MammogramAppointmentResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                TotalCount = total,
                Page = Math.Max(1, query.Page),
                PageSize = Math.Clamp(query.PageSize, 1, 200)
            };
        }

        public async Task<List<MammogramAppointmentResponse>> GetDayAsync(DateOnly day) =>
            (await _appointments.GetDayAsync(day.ToDateTime(TimeOnly.MinValue))).Select(ToResponse).ToList();

        public async Task<MammogramAppointmentDetailResponse?> GetDetailAsync(int id)
        {
            var row = await _appointments.GetRowAsync(id);
            if (row == null) return null;

            var detail = new MammogramAppointmentDetailResponse();
            Fill(detail, row);
            detail.History = (await _appointments.GetHistoryAsync(id)).Select(h => new MammogramAppointmentHistoryResponse
            {
                Id = h.Id,
                Action = h.Action,
                ResultingStatus = h.ResultingStatus,
                ActorName = h.ActorName,
                AppointmentAt = h.AppointmentAt,
                DurationMinutes = h.DurationMinutes,
                Room = h.Room,
                CallSucceeded = h.CallSucceeded,
                Note = h.Note,
                CreatedAt = h.CreatedAt
            }).ToList();
            return detail;
        }

        public Task<List<string>> GetDoctorNamesAsync() => _appointments.GetDoctorNamesAsync();

        // ---------- writes ----------

        public async Task<MammogramResult<MammogramAppointmentDetailResponse>> BookAsync(BookMammogramAppointmentDTO dto, string userId)
        {
            var name = Clean(dto.FullName);
            if (name == null || name.Length < 2) return Failed("Patient name is required.");
            if (name.Length > 200) return Failed("Patient name is longer than 200 characters.");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) return Failed("Phone number is required.");
            if (Clean(dto.PhoneCountryCode)?.Length > 6) return Failed("Country code is too long.");
            var (countryCode, phone, phoneError) = MammogramService.NormalizePhone(dto.PhoneCountryCode, dto.PhoneNumber);
            if (phoneError != null) return Failed(phoneError);

            var notes = Clean(dto.Notes);
            if (notes?.Length > 2000) return Failed("Notes are longer than 2000 characters.");

            var referring = Clean(dto.ReferringDoctor);
            var receiving = Clean(dto.ReceivingDoctor);
            if (referring?.Length > 100 || receiving?.Length > 100) return Failed("A doctor's name is longer than 100 characters.");

            var slotError = await CheckSlotAsync(dto.AppointmentAt, dto.DurationMinutes, dto.Room, excludingId: null);
            if (slotError != null) return slotError;

            var now = AppClock.Now;
            var appointment = new MammogramAppointment
            {
                FullName = name,
                PhoneCountryCode = countryCode,
                PhoneNumber = phone!,
                Notes = notes,
                Status = MammogramAppointmentStatus.Booked,
                AppointmentAt = dto.AppointmentAt!.Value,
                DurationMinutes = dto.DurationMinutes,
                Room = dto.Room,
                Procedures = (dto.Procedures ?? new()).Aggregate(MammogramProcedures.None, (all, p) => all | p),
                ReferringDoctor = referring,
                ReceivingDoctor = receiving,
                CreatedById = userId,
                CreatedAt = now
            };
            appointment.History.Add(new MammogramAppointmentHistory
            {
                ActorId = userId,
                Action = MammogramAppointmentActions.Created,
                ResultingStatus = MammogramAppointmentStatus.Booked,
                AppointmentAt = appointment.AppointmentAt,
                DurationMinutes = appointment.DurationMinutes,
                Room = appointment.Room,
                CreatedAt = now
            });

            _appointments.Add(appointment);
            await _appointments.SaveChangesAsync();
            return await Reload(appointment.Id);
        }

        public async Task<MammogramResult<MammogramAppointmentDetailResponse>> FollowUpAsync(int id, MammogramAppointmentFollowUpDTO dto, string userId)
        {
            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment == null) return Missing();

            if (dto.Status is not (MammogramAppointmentStatus.FollowUp or MammogramAppointmentStatus.Success or MammogramAppointmentStatus.Failed))
                return Failed("A follow-up ends as Follow up, Success or Failed.");

            var note = Clean(dto.Note);
            if (note?.Length > 2000) return Failed("The note is longer than 2000 characters.");
            if (dto.Status == MammogramAppointmentStatus.Failed && note == null)
                return Failed("Say why it failed.");
            if (dto.Reschedule != null && dto.Status != MammogramAppointmentStatus.FollowUp)
                return Failed("Only a follow-up can move the appointment.");

            var now = AppClock.Now;
            var entry = new MammogramAppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = userId,
                Action = MammogramAppointmentActions.FollowUp,
                ResultingStatus = dto.Status,
                Note = note,
                CreatedAt = now
            };

            if (dto.Status == MammogramAppointmentStatus.FollowUp)
            {
                entry.CallSucceeded = dto.CallSucceeded;

                if (dto.Reschedule is { } slot)
                {
                    var slotError = await CheckSlotAsync(slot.AppointmentAt, slot.DurationMinutes, slot.Room, appointment.Id);
                    if (slotError != null) return slotError;

                    appointment.AppointmentAt = slot.AppointmentAt!.Value;
                    appointment.DurationMinutes = slot.DurationMinutes;
                    appointment.Room = slot.Room;
                    entry.AppointmentAt = appointment.AppointmentAt;
                    entry.DurationMinutes = appointment.DurationMinutes;
                    entry.Room = appointment.Room;
                }
                else if (appointment.Status == MammogramAppointmentStatus.Failed
                    && await _appointments.HasOverlapAsync(appointment.AppointmentAt, End(appointment), appointment.Room, appointment.Id))
                {
                    // A failed appointment gave its slot back; taking it up
                    // again must not land on whoever was booked into it since.
                    return Conflicted($"Room {appointment.Room} has been booked at that time since this failed — pick a new time.");
                }

                appointment.ClosedAt = null;
            }
            else
            {
                appointment.ClosedAt = now;
            }

            appointment.Status = dto.Status;
            _appointments.AddHistory(entry);
            await _appointments.SaveChangesAsync();
            return await Reload(appointment.Id);
        }

        public async Task<MammogramResult<MammogramAppointmentDetailResponse>> ArriveAsync(int id, string userId)
        {
            var appointment = await _appointments.GetByIdAsync(id);
            if (appointment == null) return Missing();

            if (appointment.Status is not (MammogramAppointmentStatus.Booked or MammogramAppointmentStatus.FollowUp))
                return Conflicted("Only a booked or followed-up appointment can be marked arrived.");
            if (appointment.AppointmentAt.Date > AppClock.Today)
                return Conflicted("This appointment isn't until a later day.");

            var now = AppClock.Now;
            appointment.Status = MammogramAppointmentStatus.Arrived;
            appointment.ArrivedAt = now;
            _appointments.AddHistory(new MammogramAppointmentHistory
            {
                AppointmentId = appointment.Id,
                ActorId = userId,
                Action = MammogramAppointmentActions.Arrived,
                ResultingStatus = MammogramAppointmentStatus.Arrived,
                CreatedAt = now
            });
            await _appointments.SaveChangesAsync();
            return await Reload(appointment.Id);
        }

        // ---------- stats + export ----------

        public async Task<MammogramAppointmentStatsResponse> GetStatsAsync(int? year, int? month)
        {
            var today = AppClock.Today;
            var y = year ?? today.Year;
            var m = month is >= 1 and <= 12 ? month.Value : today.Month;
            var from = new DateTime(y, m, 1);

            var (counts, arrivedNow) = await _appointments.GetStatsAsync(from, from.AddMonths(1));

            int Of(MammogramAppointmentStatus s) => counts.Where(c => c.Status == s).Sum(c => c.Count);

            return new MammogramAppointmentStatsResponse
            {
                Year = y,
                Month = m,
                Total = counts.Sum(c => c.Count),
                Booked = Of(MammogramAppointmentStatus.Booked),
                FollowUp = Of(MammogramAppointmentStatus.FollowUp),
                Arrived = Of(MammogramAppointmentStatus.Arrived),
                Success = Of(MammogramAppointmentStatus.Success),
                Failed = Of(MammogramAppointmentStatus.Failed),
                ArrivedNow = arrivedNow,
                Procedures = Enum.GetValues<MammogramProcedures>()
                    .Where(p => p != MammogramProcedures.None)
                    .Select(p => new MammogramProcedureCount { Procedure = p, Count = counts.Where(c => c.Procedures.HasFlag(p)).Sum(c => c.Count) })
                    .Where(p => p.Count > 0)
                    .OrderByDescending(p => p.Count)
                    .ToList()
            };
        }

        // A month is a few hundred rows at most, so the report reads them once
        // and every breakdown is counted from that list: the list printed
        // under the numbers is the very rows they were counted from.
        public async Task<MammogramAppointmentReportResponse> GetMonthlyReportAsync(int? year, int? month)
        {
            var today = AppClock.Today;
            var y = year is >= 2000 and <= 9999 ? year.Value : today.Year;
            var m = month is >= 1 and <= 12 ? month.Value : today.Month;
            var from = new DateTime(y, m, 1);

            var rows = await _appointments.GetBookedAsync(from, from.AddMonths(1));

            int Of(MammogramAppointmentStatus s) => rows.Count(r => r.Status == s);
            // The report counts a patient who arrived as a success (the
            // radiology team's call, 2026-10-17): she came, whatever the
            // outcome button says yet. Only this report — the stats card
            // keeps the statuses apart.
            static bool Succeeded(MammogramAppointmentRow r) =>
                r.Status is MammogramAppointmentStatus.Success or MammogramAppointmentStatus.Arrived;
            var success = rows.Count(Succeeded);
            var failed = Of(MammogramAppointmentStatus.Failed);

            static MammogramReportGroup Group(string name, IEnumerable<MammogramAppointmentRow> of)
            {
                var list = of.ToList();
                return new MammogramReportGroup
                {
                    Name = name,
                    Total = list.Count,
                    Success = list.Count(Succeeded),
                    Failed = list.Count(r => r.Status == MammogramAppointmentStatus.Failed)
                };
            }

            // Blank names group under "", which the console labels.
            List<MammogramReportGroup> By(Func<MammogramAppointmentRow, string?> key) => rows
                .GroupBy(r => key(r) ?? "")
                .Select(g => Group(g.Key, g))
                .OrderByDescending(g => g.Total).ThenBy(g => g.Name)
                .ToList();

            return new MammogramAppointmentReportResponse
            {
                Year = y,
                Month = m,
                Total = rows.Count,
                Booked = Of(MammogramAppointmentStatus.Booked),
                FollowUp = Of(MammogramAppointmentStatus.FollowUp),
                Success = success,
                Failed = failed,
                SuccessRate = success + failed == 0 ? null : (double)success / (success + failed),
                Procedures = Enum.GetValues<MammogramProcedures>()
                    .Where(p => p != MammogramProcedures.None)
                    .Select(p => new MammogramProcedureCount { Procedure = p, Count = rows.Count(r => r.Procedures.HasFlag(p)) })
                    .Where(p => p.Count > 0)
                    .OrderByDescending(p => p.Count)
                    .ToList(),
                // Every room, an empty one included, in room order.
                Rooms = Enumerable.Range(1, RoomCount).Select(room => Group(room.ToString(), rows.Where(r => r.Room == room))).ToList(),
                BookedBy = By(r => r.CreatedByName),
                ReferringDoctors = By(r => r.ReferringDoctor),
                Items = rows.Select(ToResponse).ToList()
            };
        }

        // Both ends inclusive, by the day the appointment was booked; this
        // month when neither is given.
        public async Task<MammogramResult<(byte[] Content, string FileName)>> ExportAsync(DateOnly? from, DateOnly? to)
        {
            var today = DateOnly.FromDateTime(AppClock.Today);
            var start = from ?? new DateOnly(today.Year, today.Month, 1);
            var end = to ?? (from.HasValue ? today : start.AddMonths(1).AddDays(-1));
            if (end < start) return new() { Error = "The end date is before the start date." };
            if (end.DayNumber - start.DayNumber >= MaxExportDays) return new() { Error = "Export at most a year at a time." };

            var (items, followUps) = await _appointments.GetForExportAsync(
                start.ToDateTime(TimeOnly.MinValue), end.AddDays(1).ToDateTime(TimeOnly.MinValue));

            var content = MammogramAppointmentExcelWriter.Build(items, followUps);
            return new() { Success = true, Item = (content, $"mammogram-appointments-{start:yyyy-MM-dd}-to-{end:yyyy-MM-dd}.xlsx") };
        }

        // ---------- rules ----------

        // Null when the slot is bookable; otherwise the answer to send back.
        private async Task<MammogramResult<MammogramAppointmentDetailResponse>?> CheckSlotAsync(DateTime? at, int duration, int room, int? excludingId)
        {
            if (at is not { } start) return Failed("Pick the appointment's date and time.");
            if (start.Second != 0 || start.Millisecond != 0 || start.Minute % 15 != 0)
                return Failed("Appointments start on the quarter hour.");
            if (start.Hour < OpensAtHour || start.Hour >= ClosesAtHour)
                return Failed($"Appointments start between {OpensAtHour:00}:00 and {ClosesAtHour - 1:00}:45.");
            if (start.Date < AppClock.Today) return Failed("That day has already passed.");
            if (!Durations.Contains(duration))
                return Failed($"Session length must be one of {string.Join(", ", Durations)} minutes.");
            if (room < 1 || room > RoomCount) return Failed($"Room must be 1 or {RoomCount}.");

            if (await _appointments.HasOverlapAsync(start, start.AddMinutes(duration), room, excludingId))
                return Conflicted($"Room {room} is already booked at that time.");
            return null;
        }

        private static DateTime End(MammogramAppointment a) => a.AppointmentAt.AddMinutes(a.DurationMinutes);

        // ---------- mapping ----------

        private async Task<MammogramResult<MammogramAppointmentDetailResponse>> Reload(int id) =>
            new() { Success = true, Item = (await GetDetailAsync(id))! };

        private static MammogramAppointmentResponse ToResponse(MammogramAppointmentRow row)
        {
            var response = new MammogramAppointmentResponse();
            Fill(response, row);
            return response;
        }

        private static void Fill(MammogramAppointmentResponse r, MammogramAppointmentRow row)
        {
            r.Id = row.Id;
            r.FullName = row.FullName;
            r.PhoneCountryCode = row.PhoneCountryCode;
            r.PhoneNumber = row.PhoneNumber;
            r.WhatsAppNumber = MammogramService.WhatsAppNumber(row.PhoneCountryCode, row.PhoneNumber);
            r.Notes = row.Notes;
            r.Status = row.Status;
            r.AppointmentAt = row.AppointmentAt;
            r.DurationMinutes = row.DurationMinutes;
            r.Room = row.Room;
            r.Procedures = MammogramService.ProcedureList(row.Procedures);
            r.ReferringDoctor = row.ReferringDoctor;
            r.ReceivingDoctor = row.ReceivingDoctor;
            r.CreatedById = row.CreatedById;
            r.CreatedByName = row.CreatedByName;
            r.CreatedAt = row.CreatedAt;
            r.ArrivedAt = row.ArrivedAt;
            r.ClosedAt = row.ClosedAt;
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static MammogramResult<MammogramAppointmentDetailResponse> Failed(string error) => new() { Error = error };
        private static MammogramResult<MammogramAppointmentDetailResponse> Conflicted(string error) => new() { Conflict = true, Error = error };
        private static MammogramResult<MammogramAppointmentDetailResponse> Missing() => new() { NotFound = true };
    }
}
