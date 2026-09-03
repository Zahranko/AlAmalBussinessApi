using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class ReferalSourceRepo : IReferalSourceRepo
    {
        private readonly AppDbContext _context;

        public ReferalSourceRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ReferalSource>> GetAllAsync() =>
            await _context.Referals.OrderBy(r => r.Name).ToListAsync();

        public async Task<IEnumerable<ReferalSource>> GetActiveAsync() =>
            await _context.Referals.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        public async Task<ReferalSource?> GetByIdAsync(int id) =>
            await _context.Referals.FindAsync(id);

        public async Task<ReferalSource> CreateAsync(ReferalSource referalSource)
        {
            _context.Referals.Add(referalSource);
            await _context.SaveChangesAsync();
            return referalSource;
        }

        public async Task<ReferalSource> UpdateAsync(ReferalSource referalSource)
        {
            _context.Referals.Update(referalSource);
            await _context.SaveChangesAsync();
            return referalSource;
        }

        public async Task<bool> IsNameExist(string name, int excludeId) =>
            await _context.Referals.AnyAsync(r => r.Name == name && r.Id != excludeId);
    }
}
