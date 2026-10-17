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
                FollowUpCount = s.Records.Count(r => r.FollowUpDate != null),
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

        public async Task<(List<MammogramRecord> Items, int TotalCount)> PageAsync(MammogramRecordQuery query)
        {
            var q = _context.MammogramRecords.AsNoTracking();

            if (query.SheetId.HasValue) q = q.Where(r => r.SheetId == query.SheetId.Value);
            if (query.ExamType.HasValue) q = q.Where(r => r.ExamType == query.ExamType.Value);
            if (!string.IsNullOrWhiteSpace(query.Tech)) q = q.Where(r => r.Tech == query.Tech.Trim());
            if (query.ReminderStatus.HasValue) q = q.Where(r => r.ReminderStatus == query.ReminderStatus.Value);
            if (query.OnlyWithPhone) q = q.Where(r => r.PhoneNumber != null && r.PhoneNumber != "");

            var byFollowUp = query.FollowUpFrom.HasValue || query.FollowUpTo.HasValue;
            if (byFollowUp) q = q.Where(r => r.FollowUpDate != null);
            if (query.FollowUpFrom.HasValue) q = q.Where(r => r.FollowUpDate >= query.FollowUpFrom.Value);
            if (query.FollowUpTo.HasValue) q = q.Where(r => r.FollowUpDate <= query.FollowUpTo.Value);

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

            var total = await q.CountAsync();

            q = byFollowUp
                ? q.OrderBy(r => r.FollowUpDate).ThenBy(r => r.Id)
                : q.OrderBy(r => r.VisitDate).ThenBy(r => r.Id);

            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 2000);
            var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();

            return (items, total);
        }

        public Task<List<MammogramRecord>> GetBySheetAsync(int sheetId) =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.SheetId == sheetId)
                .OrderBy(r => r.VisitDate).ThenBy(r => r.Id)
                .ToListAsync();

        public Task<List<MammogramReminderRow>> GetDueRemindersAsync(DateOnly day, int max) =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.FollowUpDate == day
                    && r.ReminderStatus == MammogramReminderStatus.NotSent
                    && r.PhoneNumber != null && r.PhoneNumber != "")
                .OrderBy(r => r.Id)
                .Take(max)
                .Select(r => new MammogramReminderRow
                {
                    Id = r.Id,
                    PatientMrn = r.PatientMrn,
                    FullName = r.FullName,
                    PhoneCountryCode = r.PhoneCountryCode,
                    PhoneNumber = r.PhoneNumber!,
                    Recommendation = r.Recommendation,
                    FollowUpExam = r.FollowUpExam
                })
                .ToListAsync();

        public async Task<bool> ClaimReminderAsync(int id, DateTime at) =>
            await _context.MammogramRecords
                .Where(r => r.Id == id && r.ReminderStatus == MammogramReminderStatus.NotSent)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ReminderStatus, MammogramReminderStatus.Queued)
                    .SetProperty(r => r.ReminderSentAt, at)
                    .SetProperty(r => r.ReminderMessageId, (string?)null)
                    .SetProperty(r => r.ReminderError, (string?)null)) == 1;

        public Task FinishReminderClaimAsync(int id, MammogramReminderStatus status, string? messageId, string? error) =>
            _context.MammogramRecords
                .Where(r => r.Id == id && r.ReminderStatus == MammogramReminderStatus.Queued && r.ReminderMessageId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.ReminderStatus, status)
                    .SetProperty(r => r.ReminderMessageId, messageId)
                    .SetProperty(r => r.ReminderError, error)
                    // Back to NotSent means it was never sent: no stamp.
                    .SetProperty(r => r.ReminderSentAt, r => status == MammogramReminderStatus.NotSent ? null : r.ReminderSentAt));

        public Task<List<MammogramReminderInFlight>> GetRemindersInFlightAsync(DateTime since) =>
            _context.MammogramRecords.AsNoTracking()
                .Where(r => r.ReminderMessageId != null
                    && r.ReminderSentAt >= since
                    && (r.ReminderStatus == MammogramReminderStatus.Queued
                        || r.ReminderStatus == MammogramReminderStatus.Sent
                        || r.ReminderStatus == MammogramReminderStatus.Delivered))
                .Select(r => new MammogramReminderInFlight { Id = r.Id, MessageId = r.ReminderMessageId!, Status = r.ReminderStatus })
                .ToListAsync();

        public Task UpdateReminderStatusAsync(int id, string messageId, MammogramReminderStatus status, string? error) =>
            _context.MammogramRecords
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
}
