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

        public Task<List<LeadHistory>> GetByLeadAsync(int leadId) =>
            _context.LeadHistories
                .AsNoTracking()
                .Include(h => h.Actor)
                .Include(h => h.Doctor)
                .Include(h => h.ClosedReason)
                .Where(h => h.LeadId == leadId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

        public Task<List<LeadHistory>> GetFollowUpsByLeadIdsAsync(IEnumerable<int> leadIds) =>
            _context.LeadHistories
                .AsNoTracking()
                .Include(h => h.Actor)
                .Where(h => leadIds.Contains(h.LeadId) && h.Type == LeadActions.FollowUp)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

        private static IQueryable<LeadHistory> SucceededInRange(IQueryable<LeadHistory> q, DateTime from, DateTime toExclusive) =>
            q.Where(h => h.Type == LeadActions.FollowUp && h.ResultingStatus == LeadStatus.Success
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
