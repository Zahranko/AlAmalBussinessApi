using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CRM
{
    public class DoctorRepo : IDoctorRepo
    {
        private readonly AppDbContext _context;

        public DoctorRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Doctors>> GetAllAsync() =>
            await _context.Doctors.OrderBy(d => d.Name).ToListAsync();

        public async Task<IEnumerable<Doctors>> GetActiveAsync() =>
            await _context.Doctors.Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync();

        public async Task<Doctors?> GetByIdAsync(int id) =>
            await _context.Doctors.FindAsync(id);

        public async Task<Doctors> CreateAsync(Doctors doctor)
        {
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<Doctors> UpdateAsync(Doctors doctor)
        {
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync();
            return doctor;
        }

        public async Task<bool> IsNameExist(string name, int excludeId) =>
            await _context.Doctors.AnyAsync(d => d.Name == name && d.Id != excludeId);
    }
}
