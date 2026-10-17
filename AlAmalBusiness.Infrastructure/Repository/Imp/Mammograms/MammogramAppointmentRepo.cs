using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using AlAmalBusiness.Domain.Models.Mammograms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Mammograms
{
    public class MammogramAppointmentRepo : IMammogramAppointmentRepo
    {
        private readonly AppDbContext _context;

        public MammogramAppointmentRepo(AppDbContext context)
        {
            _context = context;
        }

        private static readonly Expression<Func<MammogramAppointment, MammogramAppointmentRow>> ToRow = a => new MammogramAppointmentRow
        {
            Id = a.Id,
            FullName = a.FullName,
            PhoneCountryCode = a.PhoneCountryCode,
            PhoneNumber = a.PhoneNumber,
            Notes = a.Notes,
            Status = a.Status,
            AppointmentAt = a.AppointmentAt,
            DurationMinutes = a.DurationMinutes,
            Room = a.Room,
            Procedures = a.Procedures,
            ReferringDoctor = a.ReferringDoctor,
            ReceivingDoctor = a.ReceivingDoctor,
            CreatedById = a.CreatedById,
            CreatedByName = a.CreatedBy!.UserName,
            CreatedAt = a.CreatedAt,
            ArrivedAt = a.ArrivedAt,
            ClosedAt = a.ClosedAt
        };

        private static readonly Expression<Func<MammogramAppointmentHistory, MammogramAppointmentHistoryRow>> ToHistoryRow = h => new MammogramAppointmentHistoryRow
        {
            Id = h.Id,
            AppointmentId = h.AppointmentId,
            Action = h.Action,
            ResultingStatus = h.ResultingStatus,
            ActorName = h.Actor!.UserName,
            AppointmentAt = h.AppointmentAt,
            DurationMinutes = h.DurationMinutes,
            Room = h.Room,
            CallSucceeded = h.CallSucceeded,
            Note = h.Note,
            CreatedAt = h.CreatedAt
        };

        public Task<MammogramAppointment?> GetByIdAsync(int id) =>
            _context.MammogramAppointments.FirstOrDefaultAsync(a => a.Id == id);

        public Task<MammogramAppointmentRow?> GetRowAsync(int id) =>
            _context.MammogramAppointments.AsNoTracking().Where(a => a.Id == id).Select(ToRow).FirstOrDefaultAsync();

        public Task<List<MammogramAppointmentHistoryRow>> GetHistoryAsync(int appointmentId) =>
            _context.MammogramAppointmentHistories.AsNoTracking()
                .Where(h => h.AppointmentId == appointmentId)
                .OrderBy(h => h.CreatedAt).ThenBy(h => h.Id)
                .Select(ToHistoryRow)
                .ToListAsync();

        public Task<List<MammogramAppointmentRow>> GetDayAsync(DateTime day)
        {
            var start = day.Date;
            var end = start.AddDays(1);
            return _context.MammogramAppointments.AsNoTracking()
                .Where(a => a.AppointmentAt >= start && a.AppointmentAt < end)
                .OrderBy(a => a.AppointmentAt).ThenBy(a => a.Room)
                .Select(ToRow)
                .ToListAsync();
        }

        public async Task<(List<MammogramAppointmentRow> Items, int TotalCount)> PageAsync(MammogramAppointmentQuery query)
        {
            var q = _context.MammogramAppointments.AsNoTracking();
            var today = query.Today.Date;
            var tomorrow = today.AddDays(1);

            switch (query.View)
            {
                case MammogramAppointmentView.Today:
                    q = q.Where(a => a.AppointmentAt >= today && a.AppointmentAt < tomorrow
                        && a.Status != MammogramAppointmentStatus.Success
                        && a.Status != MammogramAppointmentStatus.Failed);
                    break;
                case MammogramAppointmentView.Open:
                    q = q.Where(a => a.Status == MammogramAppointmentStatus.Booked || a.Status == MammogramAppointmentStatus.FollowUp);
                    break;
                case MammogramAppointmentView.CreatedToday:
                    q = q.Where(a => a.CreatedAt >= today && a.CreatedAt < tomorrow);
                    break;
                case MammogramAppointmentView.Mine:
                    q = q.Where(a => a.CreatedById == query.UserId);
                    break;
            }

            if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status.Value);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                var digits = new string(term.Where(char.IsDigit).ToArray());

                // Digits and phone punctuation search the phone, stored without
                // its leading zero, so "0799..." and "799..." both match.
                if (digits.Length > 0 && term.All(c => char.IsDigit(c) || " +-()".Contains(c)))
                {
                    var national = digits.TrimStart('0');
                    if (national.Length == 0) national = digits;
                    q = q.Where(a => a.PhoneNumber.Contains(national));
                }
                else
                {
                    q = q.Where(a => a.FullName.Contains(term));
                }
            }

            var total = await q.CountAsync();

            // Today's list reads like the day: earliest first. Every other
            // view is newest booking first.
            q = query.View == MammogramAppointmentView.Today
                ? q.OrderBy(a => a.AppointmentAt).ThenBy(a => a.Room)
                : q.OrderByDescending(a => a.AppointmentAt).ThenByDescending(a => a.Id);

            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 200);
            var items = await q.Skip((page - 1) * size).Take(size).Select(ToRow).ToListAsync();

            return (items, total);
        }

        // Sessions never cross midnight, so only that day's rows are
        // candidates; the end of each is computed in SQL from its own length.
        public Task<bool> HasOverlapAsync(DateTime start, DateTime end, int room, int? excludingId)
        {
            var dayStart = start.Date;
            var dayEnd = dayStart.AddDays(1);
            return _context.MammogramAppointments.AnyAsync(a =>
                a.Room == room
                && a.Status != MammogramAppointmentStatus.Failed
                && (excludingId == null || a.Id != excludingId)
                && a.AppointmentAt >= dayStart && a.AppointmentAt < dayEnd
                && a.AppointmentAt < end
                && a.AppointmentAt.AddMinutes(a.DurationMinutes) > start);
        }

        public async Task<(List<MammogramAppointmentCount> Counts, int ArrivedNow)> GetStatsAsync(DateTime from, DateTime to)
        {
            var counts = await _context.MammogramAppointments.AsNoTracking()
                .Where(a => a.CreatedAt >= from && a.CreatedAt < to)
                .GroupBy(a => new { a.Status, a.Procedures })
                .Select(g => new MammogramAppointmentCount { Status = g.Key.Status, Procedures = g.Key.Procedures, Count = g.Count() })
                .ToListAsync();

            var arrivedNow = await _context.MammogramAppointments
                .CountAsync(a => a.Status == MammogramAppointmentStatus.Arrived);

            return (counts, arrivedNow);
        }

        public async Task<(List<MammogramAppointmentRow> Items, List<MammogramAppointmentHistoryRow> FollowUps)> GetForExportAsync(DateTime from, DateTime to)
        {
            var items = await _context.MammogramAppointments.AsNoTracking()
                .Where(a => a.CreatedAt >= from && a.CreatedAt < to)
                .OrderBy(a => a.Id)
                .Select(ToRow)
                .ToListAsync();

            // Same range predicate rather than a list of ids, so a busy month
            // can't run into SQL Server's parameter limit.
            var followUps = await _context.MammogramAppointmentHistories.AsNoTracking()
                .Where(h => h.Action == MammogramAppointmentActions.FollowUp
                    && h.Appointment!.CreatedAt >= from && h.Appointment.CreatedAt < to)
                .OrderBy(h => h.CreatedAt).ThenBy(h => h.Id)
                .Select(ToHistoryRow)
                .ToListAsync();

            return (items, followUps);
        }

        public Task<List<MammogramAppointmentRow>> GetBookedAsync(DateTime from, DateTime to) =>
            _context.MammogramAppointments.AsNoTracking()
                .Where(a => a.CreatedAt >= from && a.CreatedAt < to)
                .OrderBy(a => a.AppointmentAt).ThenBy(a => a.Room)
                .Select(ToRow)
                .ToListAsync();

        public async Task<List<string>> GetDoctorNamesAsync()
        {
            var referring = _context.MammogramAppointments.Where(a => a.ReferringDoctor != null && a.ReferringDoctor != "").Select(a => a.ReferringDoctor!);
            var receiving = _context.MammogramAppointments.Where(a => a.ReceivingDoctor != null && a.ReceivingDoctor != "").Select(a => a.ReceivingDoctor!);
            return await referring.Union(receiving).OrderBy(n => n).ToListAsync();
        }

        public void Add(MammogramAppointment appointment) => _context.MammogramAppointments.Add(appointment);

        public void AddHistory(MammogramAppointmentHistory entry) => _context.MammogramAppointmentHistories.Add(entry);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
