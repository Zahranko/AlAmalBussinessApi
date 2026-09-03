using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class ProcedureRepo : IProcedureRepo
    {
        private readonly AppDbContext _context;

        public ProcedureRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Procedures>> GetAllAsync() =>
            await _context.Procedures.OrderBy(p => p.Name).ToListAsync();

        public async Task<IEnumerable<Procedures>> GetActiveAsync() =>
            await _context.Procedures.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

        public async Task<Procedures?> GetByIdAsync(int id) =>
            await _context.Procedures.FindAsync(id);

        public async Task<Procedures> CreateAsync(Procedures procedure)
        {
            _context.Procedures.Add(procedure);
            await _context.SaveChangesAsync();
            return procedure;
        }

        public async Task<Procedures> UpdateAsync(Procedures procedure)
        {
            _context.Procedures.Update(procedure);
            await _context.SaveChangesAsync();
            return procedure;
        }

        public async Task<bool> IsNameExist(string name, int excludeId) =>
            await _context.Procedures.AnyAsync(p => p.Name == name && p.Id != excludeId);
    }
}
