using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class ClosedReasonRepo : IClosedReasonRepo
    {
        private readonly AppDbContext _context;

        public ClosedReasonRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClosedReason>> GetAllAsync() =>
            await _context.ClosedReasons.OrderBy(r => r.Name).ToListAsync();

        public async Task<IEnumerable<ClosedReason>> GetActiveAsync() =>
            await _context.ClosedReasons.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        public async Task<ClosedReason?> GetByIdAsync(int id) =>
            await _context.ClosedReasons.FindAsync(id);

        public async Task<ClosedReason> CreateAsync(ClosedReason closedReason)
        {
            _context.ClosedReasons.Add(closedReason);
            await _context.SaveChangesAsync();
            return closedReason;
        }

        public async Task<ClosedReason> UpdateAsync(ClosedReason closedReason)
        {
            _context.ClosedReasons.Update(closedReason);
            await _context.SaveChangesAsync();
            return closedReason;
        }

        public async Task<bool> IsNameExist(string name, int excludeId) =>
            await _context.ClosedReasons.AnyAsync(r => r.Name == name && r.Id != excludeId);
    }
}
