using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class LeadRepo : ILeadRepo
    {
        private readonly AppDbContext _context;

        public LeadRepo(AppDbContext context)
        {
            _context = context;
        }

        private IQueryable<Lead> WithLeadIncludes() =>
            _context.Leads
                .Include(l => l.Doctor)
                .Include(l => l.Procedure)
                .Include(l => l.Referal)
                .Include(l => l.CreatedBy)
                .Include(l => l.ClaimedBy)
                .Include(l => l.ClosedReason);

        private static IQueryable<Lead> ExcludeCompleted(IQueryable<Lead> q) =>
            q.Where(l => l.Status != LeadStatus.Success && l.Status != LeadStatus.Closed);

        // Lead.CreatedDate defaults to DateTime.Now (local), not UtcNow — compare
        // against local day boundaries to match, no UTC conversion needed.
        private static IQueryable<Lead> ApplyDateRange(IQueryable<Lead> q, DateTime? from, DateTime? to)
        {
            if (from.HasValue)
                q = q.Where(l => l.CreatedDate >= from.Value.Date);
            if (to.HasValue)
                q = q.Where(l => l.CreatedDate < to.Value.Date.AddDays(1));
            return q;
        }

        public async Task<Lead> CreateLeadAsync(Lead lead)
        {
            await _context.Leads.AddAsync(lead);
            await _context.SaveChangesAsync();
            return lead;
        }

        public async Task DeleteLeadAsync(Lead lead)
        {
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();
        }

        public Task<Lead?> GetLeadByIdAsync(int id) =>
            WithLeadIncludes().FirstOrDefaultAsync(l => l.Id == id);

        public Task<Lead?> GetLeadDetailAsync(int id) =>
            WithLeadIncludes().AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

        public Task<List<Lead>> GetAllLeadsAsync(bool excludeCompleted = false)
        {
            var q = WithLeadIncludes().AsNoTracking().OrderByDescending(l => l.CreatedDate);
            return (excludeCompleted ? ExcludeCompleted(q) : q).ToListAsync();
        }

        public Task<List<Lead>> GetMineAsync(string userId, bool excludeCompleted = false)
        {
            var q = WithLeadIncludes().AsNoTracking()
                .Where(l => l.ClaimedById == userId)
                .OrderByDescending(l => l.CreatedDate);
            return (excludeCompleted ? ExcludeCompleted(q) : q).ToListAsync();
        }

        public Task<List<Lead>> GetCreatedByMeAsync(string userId, bool excludeCompleted = false)
        {
            var q = WithLeadIncludes().AsNoTracking()
                .Where(l => l.CreatedById == userId)
                .OrderByDescending(l => l.CreatedDate);
            return (excludeCompleted ? ExcludeCompleted(q) : q).ToListAsync();
        }

        public Task<(List<Lead> Items, int TotalCount)> GetPagedAsync(LeadListQuery query) =>
            PageLeadsAsync(WithLeadIncludes().AsNoTracking(), query);

        public Task<(List<Lead> Items, int TotalCount)> GetCreatedByMePagedAsync(string userId, LeadListQuery query) =>
            PageLeadsAsync(WithLeadIncludes().AsNoTracking().Where(l => l.CreatedById == userId), query);

        private static async Task<(List<Lead> Items, int TotalCount)> PageLeadsAsync(IQueryable<Lead> q, LeadListQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                // Phone search ignores a leading zero either way — searching "78..."
                // must still find a stored "078..." and vice versa.
                var termNoZero = term.TrimStart('0');
                q = q.Where(l =>
                    (l.Name != null && l.Name.Contains(term)) ||
                    (l.NickName != null && l.NickName.Contains(term)) ||
                    (l.PhoneNum != null && (l.PhoneNum.Contains(term) || l.PhoneNum.Contains(termNoZero))));
            }

            if (query.ClaimedByUserId != null)
                q = q.Where(l => l.ClaimedById == query.ClaimedByUserId);

            if (query.CreatedByUserId != null)
                q = q.Where(l => l.CreatedById == query.CreatedByUserId);

            if (query.DoctorId.HasValue)
                q = q.Where(l => l.DoctorId == query.DoctorId.Value);

            if (query.UnclaimedOnly)
                q = q.Where(l => l.ClaimedById == null);

            if (query.TodayOnly)
            {
                var today = DateTime.Now.Date;
                q = q.Where(l => l.CreatedDate >= today && l.CreatedDate < today.AddDays(1));
            }

            if (query.ExactDate.HasValue)
                q = ApplyDateRange(q, query.ExactDate, query.ExactDate);

            if (query.Status.HasValue)
                q = q.Where(l => l.Status == query.Status.Value);
            else if (query.OnlyCompleted)
                q = q.Where(l => l.Status == LeadStatus.Success || l.Status == LeadStatus.Closed);
            else if (query.ExcludeCompletedByDefault)
                q = ExcludeCompleted(q);

            var totalCount = await q.CountAsync();
            var items = await q
                .OrderByDescending(l => l.CreatedDate)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public Task<int> CountAllAsync() => _context.Leads.CountAsync();

        public async Task<Dictionary<LeadStatus, int>> GetStatusCountsAsync(DateTime? from = null, DateTime? to = null)
        {
            var groups = await ApplyDateRange(_context.Leads, from, to)
                .GroupBy(l => l.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            return groups.ToDictionary(g => g.Status, g => g.Count);
        }

        public async Task<Dictionary<int, int>> GetReferralSourceCountsAsync()
        {
            var groups = await _context.Leads
                .GroupBy(l => l.ReferalId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();
            return groups.ToDictionary(g => g.Id, g => g.Count);
        }

        public async Task<List<(string UserId, string? Username, int Total, int Success, int Closed)>> GetLeadCountsByCreatorAsync()
        {
            var groups = await _context.Leads
                .Where(l => l.CreatedById != null)
                .GroupBy(l => l.CreatedById!)
                .Select(g => new
                {
                    UserId = g.Key,
                    Username = g.Max(l => l.CreatedBy!.UserName),
                    Total = g.Count(),
                    Success = g.Count(l => l.Status == LeadStatus.Success),
                    Closed = g.Count(l => l.Status == LeadStatus.Closed)
                })
                .ToListAsync();
            return groups.Select(g => (g.UserId, g.Username, g.Total, g.Success, g.Closed)).ToList();
        }

        public Task<List<Lead>> GetByDoctorAsync(int doctorId, DateTime? from = null, DateTime? to = null)
        {
            var q = ApplyDateRange(WithLeadIncludes().AsNoTracking().Where(l => l.DoctorId == doctorId), from, to);
            return q.OrderByDescending(l => l.CreatedDate).ToListAsync();
        }

        public async Task<List<(int ProcedureId, int Total, int Pending, int Waiting, int Success, int Closed)>> GetLeadCountsByProcedureAsync(DateTime? from = null, DateTime? to = null)
        {
            var groups = await ApplyDateRange(_context.Leads, from, to)
                .GroupBy(l => l.ProcedureId)
                .Select(g => new
                {
                    ProcedureId = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(l => l.Status == LeadStatus.Pending),
                    Waiting = g.Count(l => l.Status == LeadStatus.Waiting),
                    Success = g.Count(l => l.Status == LeadStatus.Success),
                    Closed = g.Count(l => l.Status == LeadStatus.Closed)
                })
                .ToListAsync();
            return groups.Select(g => (g.ProcedureId, g.Total, g.Pending, g.Waiting, g.Success, g.Closed)).ToList();
        }

        public async Task<List<(int DoctorId, int Total, int Pending, int Waiting, int Success, int Closed, List<(int ProcedureId, int Count)> Procedures)>> GetDoctorStatsWithProceduresAsync(DateTime? from = null, DateTime? to = null)
        {
            // Ask SQL to group by Doctor + Procedure + Status simultaneously, then
            // aggregate into the nested shape in memory (mirrors old CRMS's two-step
            // approach — a single nested GroupBy doesn't translate to SQL).
            var rawCounts = await ApplyDateRange(_context.Leads.Where(l => l.DoctorId != null), from, to)
                .GroupBy(l => new { l.DoctorId, l.ProcedureId, l.Status })
                .Select(g => new
                {
                    DoctorId = g.Key.DoctorId!.Value,
                    ProcedureId = g.Key.ProcedureId,
                    Status = g.Key.Status,
                    Count = g.Count()
                })
                .ToListAsync();

            return rawCounts
                .GroupBy(r => r.DoctorId)
                .Select(g =>
                {
                    var procedures = g
                        .GroupBy(x => x.ProcedureId)
                        .Select(pg => (ProcedureId: pg.Key, Count: pg.Sum(x => x.Count)))
                        .OrderByDescending(p => p.Count)
                        .ToList();

                    return (
                        DoctorId: g.Key,
                        Total: g.Sum(x => x.Count),
                        Pending: g.Where(x => x.Status == LeadStatus.Pending).Sum(x => x.Count),
                        Waiting: g.Where(x => x.Status == LeadStatus.Waiting).Sum(x => x.Count),
                        Success: g.Where(x => x.Status == LeadStatus.Success).Sum(x => x.Count),
                        Closed: g.Where(x => x.Status == LeadStatus.Closed).Sum(x => x.Count),
                        Procedures: procedures
                    );
                })
                .ToList();
        }

        public Task<int> CountCreatedInRangeAsync(DateTime from, DateTime toExclusive) =>
            _context.Leads.CountAsync(l => l.CreatedDate >= from && l.CreatedDate < toExclusive);

        public async Task<Dictionary<DateTime, int>> GetCreatedDailyCountsAsync(DateTime from, DateTime toExclusive)
        {
            var groups = await _context.Leads
                .Where(l => l.CreatedDate >= from && l.CreatedDate < toExclusive)
                .GroupBy(l => l.CreatedDate!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();
            return groups.ToDictionary(g => g.Day, g => g.Count);
        }

        public async Task<List<(string UserId, string? Username, int Count)>> GetCreatedCountsByUserInRangeAsync(DateTime from, DateTime toExclusive)
        {
            var groups = await _context.Leads
                .Where(l => l.CreatedById != null && l.CreatedDate >= from && l.CreatedDate < toExclusive)
                .GroupBy(l => l.CreatedById!)
                .Select(g => new { UserId = g.Key, Username = g.Max(l => l.CreatedBy!.UserName), Count = g.Count() })
                .ToListAsync();
            return groups.Select(g => (g.UserId, g.Username, g.Count)).ToList();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
