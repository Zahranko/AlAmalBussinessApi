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
    public class LeadHistoryRepo : ILeadHistoryRepo
    {
        private readonly AppDbContext _context;

        public LeadHistoryRepo(AppDbContext context)
        {
            _context = context;
        }

        public void Add(LeadHistory history) => _context.LeadHistories.Add(history);

        // Projected: names through the navigations, no AspNetUsers rows.
        public Task<List<LeadHistoryRow>> GetByLeadAsync(int leadId) =>
            _context.LeadHistories
                .AsNoTracking()
                .Where(h => h.LeadId == leadId)
                .OrderBy(h => h.CreatedAt)
                .Select(h => new LeadHistoryRow
                {
                    Id = h.Id,
                    Type = h.Type,
                    ResultingStatus = h.ResultingStatus,
                    ActorName = h.Actor!.UserName,
                    ActionDate = h.ActionDate,
                    DoctorName = h.Doctor!.Name,
                    ClosedReasonName = h.ClosedReason!.Name,
                    Note = h.Note,
                    CreatedAt = h.CreatedAt
                })
                .ToListAsync();

        // Same doctor and CreatedDate bounds as LeadRepo.GetByDoctorAsync,
        // applied through the join rather than an IN list of lead ids (which
        // hit SQL Server's 2100-parameter ceiling on a big doctor). Rooted at
        // LeadHistories, so deleted leads are excluded explicitly.
        public Task<List<LeadFollowUpRow>> GetFollowUpsForDoctorAsync(int doctorId, DateTime? from, DateTime? to)
        {
            var q = _context.LeadHistories.AsNoTracking()
                .Where(h => h.Type == LeadActions.FollowUp && h.Lead!.DoctorId == doctorId && !h.Lead.IsDeleted);

            if (from.HasValue)
            {
                var start = from.Value.Date;
                q = q.Where(h => h.Lead!.CreatedDate >= start);
            }
            if (to.HasValue)
            {
                var endExclusive = to.Value.Date.AddDays(1);
                q = q.Where(h => h.Lead!.CreatedDate < endExclusive);
            }

            return q
                .OrderBy(h => h.CreatedAt)
                .Select(h => new LeadFollowUpRow
                {
                    LeadId = h.LeadId,
                    ActionDate = h.ActionDate,
                    CreatedAt = h.CreatedAt,
                    ActorName = h.Actor!.UserName,
                    ResultingStatus = h.ResultingStatus,
                    Note = h.Note
                })
                .ToListAsync();
        }

        private static IQueryable<LeadHistory> SucceededInRange(IQueryable<LeadHistory> q, DateTime from, DateTime toExclusive) =>
            // Queried from LeadHistories directly, so the Lead query filter
            // doesn't apply by itself — exclude soft-deleted leads explicitly.
            q.Where(h => !h.Lead!.IsDeleted
                && h.Type == LeadActions.FollowUp && h.ResultingStatus == LeadStatus.Success
                && (h.ActionDate ?? h.CreatedAt) >= from && (h.ActionDate ?? h.CreatedAt) < toExclusive);

        public Task<int> CountSucceededInRangeAsync(DateTime from, DateTime toExclusive) =>
            SucceededInRange(_context.LeadHistories, from, toExclusive).CountAsync();

        public async Task<Dictionary<DateTime, int>> GetSucceededDailyCountsAsync(DateTime from, DateTime toExclusive)
        {
            var groups = await SucceededInRange(_context.LeadHistories, from, toExclusive)
                .GroupBy(h => (h.ActionDate ?? h.CreatedAt).Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();
            return groups.ToDictionary(g => g.Day, g => g.Count);
        }
    }
}
