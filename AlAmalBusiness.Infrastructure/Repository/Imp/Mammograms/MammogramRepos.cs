using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.Mammograms;
using AlAmalBusiness.Domain.Models.Mammograms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Mammograms
{
    public class MammogramSheetRepo : IMammogramSheetRepo
    {
        private readonly AppDbContext _context;

        public MammogramSheetRepo(AppDbContext context)
        {
            _context = context;
        }

        // Counts come from correlated subqueries inside the same SELECT, so the
        // sheet list is one round trip with no record rows.
        private IQueryable<MammogramSheetRow> Rows() =>
            _context.MammogramSheets.AsNoTracking().Select(s => new MammogramSheetRow
            {
                Id = s.Id,
                Year = s.Year,
                Month = s.Month,
                CreatedAt = s.CreatedAt,
                CreatedByName = s.CreatedBy!.UserName,
                RecordCount = s.Records.Count(),
                FollowUpCount = s.Records.Count(r => r.FollowUpDate != null || r.FollowUpDate2 != null),
                ContactedCount = s.Records.Count(r => r.ContactedAt != null || r.ContactNote != null)
            });

        public Task<List<MammogramSheetRow>> GetAllAsync() =>
            Rows().OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ToListAsync();

        public Task<MammogramSheetRow?> GetRowAsync(int id) =>
            Rows().FirstOrDefaultAsync(s => s.Id == id);

        public Task<MammogramSheet?> GetByIdAsync(int id) =>
            _context.MammogramSheets.FirstOrDefaultAsync(s => s.Id == id);

        public Task<bool> ExistsAsync(int year, int month) =>
            _context.MammogramSheets.AnyAsync(s => s.Year == year && s.Month == month);

        public Task<bool> HasRecordsAsync(int id) =>
            _context.MammogramRecords.AnyAsync(r => r.SheetId == id);

        public async Task CreateAsync(MammogramSheet sheet)
        {
            _context.MammogramSheets.Add(sheet);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(MammogramSheet sheet)
        {
            _context.MammogramSheets.Remove(sheet);
            await _context.SaveChangesAsync();
        }
    }

    public class MammogramRecordRepo : IMammogramRecordRepo
    {
        private readonly AppDbContext _context;

        public MammogramRecordRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<MammogramRecord?> GetByIdAsync(int id) =>
            _context.MammogramRecords.FirstOrDefaultAsync(r => r.Id == id);

        public Task<List<MammogramRecord>> GetByIdsAsync(int sheetId, List<int> ids) =>
            _context.MammogramRecords.Where(r => r.SheetId == sheetId && ids.Contains(r.Id)).ToListAsync();

        // The filters the sheet view and the follow-up list share.
        private IQueryable<MammogramRecord> Filtered(MammogramRecordQuery query)
        {
            var q = _context.MammogramRecords.AsNoTracking();

            if (query.SheetId.HasValue) q = q.Where(r => r.SheetId == query.SheetId.Value);
            if (query.ExamType.HasValue) q = q.Where(r => r.ExamType == query.ExamType.Value);
            if (!string.IsNullOrWhiteSpace(query.Tech)) q = q.Where(r => r.Tech == query.Tech.Trim());
            if (query.OnlyWithPhone) q = q.Where(r => r.PhoneNumber != null && r.PhoneNumber != "");

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                var digits = new string(term.Where(char.IsDigit).ToArray());

                // A term of digits and phone punctuation is an MRN or a phone
                // number; the phone side drops the leading zero the way the
                // number is stored, so "0799..." and "799..." both match.
                if (digits.Length > 0 && term.All(c => char.IsDigit(c) || " +-()".Contains(c)))
                {
                    var national = digits.TrimStart('0');
                    if (national.Length == 0) national = digits;
                    q = q.Where(r => r.PatientMrn.Contains(digits) || r.PhoneNumber!.Contains(national));
                }
                else
                {
                    q = q.Where(r => r.FullName.Contains(term));
                }
            }

            return q;
        }

        public async Task<(List<MammogramRecord> Items, int TotalCount)> PageAsync(MammogramRecordQuery query)
        {
            var q = Filtered(query);
            var total = await q.CountAsync();

            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 2000);
            var items = await q.OrderBy(r => r.VisitDate).ThenBy(r => r.Id)
                .Skip((page - 1) * size).Take(size).ToListAsync();

            return (items, total);
        }

        private class FollowUpKey
        {
            public int Id { get; set; }
            public int Slot { get; set; }
            public DateOnly Due { get; set; }
        }

        // Each follow-up is its own line: the two slots are queried apart and
        // UNION ALL'd as (id, slot, date) keys, the page is cut on the keys,
        // and only that page's rows are loaded — two round trips, no rows
        // outside the page.
        public async Task<(List<(MammogramRecord Record, int Slot)> Items, int TotalCount)> PageFollowUpsAsync(MammogramRecordQuery query)
        {
            var q = Filtered(query);
            var from = query.FollowUpFrom ?? DateOnly.MinValue;
            var to = query.FollowUpTo ?? DateOnly.MaxValue;
            var status = query.ReminderStatus;

            var first = q.Where(r => r.FollowUpDate != null && r.FollowUpDate >= from && r.FollowUpDate <= to);
            if (status.HasValue) first = first.Where(r => r.ReminderStatus == status.Value);
            var second = q.Where(r => r.FollowUpDate2 != null && r.FollowUpDate2 >= from && r.FollowUpDate2 <= to);
            if (status.HasValue) second = second.Where(r => r.Reminder2Status == status.Value);

            var keys = first.Select(r => new FollowUpKey { Id = r.Id, Slot = 1, Due = r.FollowUpDate!.Value })
                .Concat(second.Select(r => new FollowUpKey { Id = r.Id, Slot = 2, Due = r.FollowUpDate2!.Value }));

            var total = await keys.CountAsync();

            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 2000);
            var pageKeys = await keys.OrderBy(k => k.Due).ThenBy(k => k.Id).ThenBy(k => k.Slot)
                .Skip((page - 1) * size).Take(size).ToListAsync();

            var ids = pageKeys.Select(k => k.Id).Distinct().ToList();
            var rows = ids.Count == 0
                ? new Dictionary<int, MammogramRecord>()
                : await _context.MammogramRecords.AsNoTracking().Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id);

            var items = pageKeys.Where(k => rows.ContainsKey(k.Id)).Select(k => (rows[k.Id], k.Slot)).ToList();
            return (items, total);
        }

        public Task<List<MammogramRecord>> GetByVisitRangeAsync(DateOnly from, DateOnly to, int max) =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.VisitDate >= from && r.VisitDate <= to)
                .OrderBy(r => r.VisitDate).ThenBy(r => r.Id)
                .Take(max)
                .ToListAsync();

        public Task<List<MammogramRecord>> GetBySheetAsync(int sheetId) =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.SheetId == sheetId)
                .OrderBy(r => r.VisitDate).ThenBy(r => r.Id)
                .ToListAsync();

        public async Task<List<MammogramReminderRow>> GetDueRemindersAsync(DateOnly day, int max)
        {
            var withPhone = _context.MammogramRecords.AsNoTracking()
                .Where(r => r.PhoneNumber != null && r.PhoneNumber != "");

            var first = await withPhone
                .Where(r => r.FollowUpDate == day && r.ReminderStatus == MammogramReminderStatus.NotSent)
                .OrderBy(r => r.Id)
                .Take(max)
                .Select(r => new MammogramReminderRow
                {
                    Id = r.Id,
                    Slot = 1,
                    PatientMrn = r.PatientMrn,
                    FullName = r.FullName,
                    PhoneCountryCode = r.PhoneCountryCode,
                    PhoneNumber = r.PhoneNumber!,
                    Recommendation = r.Recommendation,
                    FollowUpExam = r.FollowUpExam
                })
                .ToListAsync();

            var second = await withPhone
                .Where(r => r.FollowUpDate2 == day && r.Reminder2Status == MammogramReminderStatus.NotSent)
                .OrderBy(r => r.Id)
                .Take(Math.Max(0, max - first.Count))
                .Select(r => new MammogramReminderRow
                {
                    Id = r.Id,
                    Slot = 2,
                    PatientMrn = r.PatientMrn,
                    FullName = r.FullName,
                    PhoneCountryCode = r.PhoneCountryCode,
                    PhoneNumber = r.PhoneNumber!,
                    Recommendation = r.Recommendation,
                    FollowUpExam = r.FollowUpExam2
                })
                .ToListAsync();

            return first.Concat(second).ToList();
        }

        public async Task<bool> ClaimReminderAsync(int id, int slot, DateTime at) =>
            slot == 2
                ? await _context.MammogramRecords
                    .Where(r => r.Id == id && r.Reminder2Status == MammogramReminderStatus.NotSent)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Reminder2Status, MammogramReminderStatus.Queued)
                        .SetProperty(r => r.Reminder2SentAt, at)
                        .SetProperty(r => r.Reminder2MessageId, (string?)null)
                        .SetProperty(r => r.Reminder2Error, (string?)null)) == 1
                : await _context.MammogramRecords
                    .Where(r => r.Id == id && r.ReminderStatus == MammogramReminderStatus.NotSent)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.ReminderStatus, MammogramReminderStatus.Queued)
                        .SetProperty(r => r.ReminderSentAt, at)
                        .SetProperty(r => r.ReminderMessageId, (string?)null)
                        .SetProperty(r => r.ReminderError, (string?)null)) == 1;

        // Back to NotSent means it was never sent: no stamp.
        public Task FinishReminderClaimAsync(int id, int slot, MammogramReminderStatus status, string? messageId, string? error) =>
            slot == 2
                ? _context.MammogramRecords
                    .Where(r => r.Id == id && r.Reminder2Status == MammogramReminderStatus.Queued && r.Reminder2MessageId == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Reminder2Status, status)
                        .SetProperty(r => r.Reminder2MessageId, messageId)
                        .SetProperty(r => r.Reminder2Error, error)
                        .SetProperty(r => r.Reminder2SentAt, r => status == MammogramReminderStatus.NotSent ? null : r.Reminder2SentAt))
                : _context.MammogramRecords
                    .Where(r => r.Id == id && r.ReminderStatus == MammogramReminderStatus.Queued && r.ReminderMessageId == null)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.ReminderStatus, status)
                        .SetProperty(r => r.ReminderMessageId, messageId)
                        .SetProperty(r => r.ReminderError, error)
                        .SetProperty(r => r.ReminderSentAt, r => status == MammogramReminderStatus.NotSent ? null : r.ReminderSentAt));

        public async Task<List<MammogramReminderInFlight>> GetRemindersInFlightAsync(DateTime since)
        {
            var first = await _context.MammogramRecords.AsNoTracking()
                .Where(r => r.ReminderMessageId != null
                    && r.ReminderSentAt >= since
                    && (r.ReminderStatus == MammogramReminderStatus.Queued
                        || r.ReminderStatus == MammogramReminderStatus.Sent
                        || r.ReminderStatus == MammogramReminderStatus.Delivered))
                .Select(r => new MammogramReminderInFlight { Id = r.Id, Slot = 1, MessageId = r.ReminderMessageId!, Status = r.ReminderStatus })
                .ToListAsync();

            var second = await _context.MammogramRecords.AsNoTracking()
                .Where(r => r.Reminder2MessageId != null
                    && r.Reminder2SentAt >= since
                    && (r.Reminder2Status == MammogramReminderStatus.Queued
                        || r.Reminder2Status == MammogramReminderStatus.Sent
                        || r.Reminder2Status == MammogramReminderStatus.Delivered))
                .Select(r => new MammogramReminderInFlight { Id = r.Id, Slot = 2, MessageId = r.Reminder2MessageId!, Status = r.Reminder2Status })
                .ToListAsync();

            return first.Concat(second).ToList();
        }

        public Task UpdateReminderStatusAsync(int id, int slot, string messageId, MammogramReminderStatus status, string? error) =>
            slot == 2
                ? _context.MammogramRecords
                    .Where(r => r.Id == id && r.Reminder2MessageId == messageId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.Reminder2Status, status)
                        .SetProperty(r => r.Reminder2Error, error))
                : _context.MammogramRecords
                    .Where(r => r.Id == id && r.ReminderMessageId == messageId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(r => r.ReminderStatus, status)
                        .SetProperty(r => r.ReminderError, error));

        public Task<List<string>> GetTechNamesAsync() =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.Tech != null && r.Tech != "")
                .Select(r => r.Tech!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

        public void Add(MammogramRecord record) => _context.MammogramRecords.Add(record);

        public void Remove(MammogramRecord record) => _context.MammogramRecords.Remove(record);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }

    public class MammogramReportRepo : IMammogramReportRepo
    {
        private readonly AppDbContext _context;

        public MammogramReportRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<MammogramReportRow>> GetAllAsync() =>
            _context.MammogramReports.AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new MammogramReportRow
                {
                    Id = r.Id,
                    Name = r.Name,
                    Definition = r.Definition,
                    CreatedByName = r.CreatedBy!.UserName,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToListAsync();

        public Task<MammogramReport?> GetByIdAsync(int id) =>
            _context.MammogramReports.FirstOrDefaultAsync(r => r.Id == id);

        public Task<bool> NameExistsAsync(string name, int? excludeId) =>
            _context.MammogramReports.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId));

        public void Add(MammogramReport report) => _context.MammogramReports.Add(report);

        public void Remove(MammogramReport report) => _context.MammogramReports.Remove(report);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
