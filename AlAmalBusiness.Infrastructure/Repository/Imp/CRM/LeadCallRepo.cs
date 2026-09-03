using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class LeadCallRepo : ILeadCallRepo
    {
        private readonly AppDbContext _context;

        public LeadCallRepo(AppDbContext context)
        {
            _context = context;
        }

        public void Add(LeadCall call) => _context.LeadCalls.Add(call);

        public Task<LeadCall?> GetByIdAsync(int id) =>
            _context.LeadCalls.FirstOrDefaultAsync(c => c.Id == id);

        public Task<List<LeadCall>> GetByLeadAsync(int leadId) =>
            _context.LeadCalls
                .AsNoTracking()
                .Include(c => c.Actor)
                .Where(c => c.LeadId == leadId)
                .OrderBy(c => c.Date)
                .ToListAsync();

        public Task<int> CountByLeadAsync(int leadId) =>
            _context.LeadCalls.CountAsync(c => c.LeadId == leadId);

        // Each lead has at most 6 calls (MaxCallsPerLead in LeadService), so
        // pulling every call row for the requested leads and grouping
        // in-memory is bounded and simpler than a per-group top-1 subquery.
        public async Task<Dictionary<int, LeadCall>> GetLastCallsByLeadIdsAsync(IEnumerable<int> leadIds)
        {
            var ids = leadIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, LeadCall>();

            var calls = await _context.LeadCalls
                .AsNoTracking()
                .Where(c => ids.Contains(c.LeadId))
                .ToListAsync();

            return calls
                .GroupBy(c => c.LeadId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.CreatedAt).First());
        }
    }
}
