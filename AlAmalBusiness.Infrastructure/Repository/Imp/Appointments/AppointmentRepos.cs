using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.Appointments
{
    public class AppointmentRequestRepo : IAppointmentRequestRepo
    {
        private readonly AppDbContext _context;

        public AppointmentRequestRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AppointmentRequest> CreateAsync(AppointmentRequest request)
        {
            _context.AppointmentRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }

    public class AppointmentProcedureRepo : IAppointmentProcedureRepo
    {
        private readonly AppDbContext _context;

        public AppointmentProcedureRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AppointmentProcedure>> GetAllAsync() =>
            _context.AppointmentProcedures.AsNoTracking().OrderBy(p => p.Name).ToListAsync();

        public Task<List<AppointmentProcedure>> GetActiveAsync() =>
            _context.AppointmentProcedures.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync();

        public Task<AppointmentProcedure?> GetByIdAsync(int id) =>
            _context.AppointmentProcedures.FirstOrDefaultAsync(p => p.Id == id);

        public async Task CreateAsync(AppointmentProcedure procedure)
        {
            _context.AppointmentProcedures.Add(procedure);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int excludeId) =>
            _context.AppointmentProcedures.AnyAsync(p => p.Name == name && p.Id != excludeId);
    }

    public class AppointmentReferralSourceRepo : IAppointmentReferralSourceRepo
    {
        private readonly AppDbContext _context;

        public AppointmentReferralSourceRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AppointmentReferralSource>> GetAllAsync() =>
            _context.AppointmentReferralSources.AsNoTracking().OrderBy(r => r.Name).ToListAsync();

        public Task<List<AppointmentReferralSource>> GetActiveAsync() =>
            _context.AppointmentReferralSources.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();

        public Task<AppointmentReferralSource?> GetByIdAsync(int id) =>
            _context.AppointmentReferralSources.FirstOrDefaultAsync(r => r.Id == id);

        public async Task CreateAsync(AppointmentReferralSource source)
        {
            _context.AppointmentReferralSources.Add(source);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<bool> IsNameExist(string name, int excludeId) =>
            _context.AppointmentReferralSources.AnyAsync(r => r.Name == name && r.Id != excludeId);
    }

    public class AppointmentEmailRepo : IAppointmentEmailRepo
    {
        private readonly AppDbContext _context;

        public AppointmentEmailRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<AppointmentNotificationEmail>> GetAllAsync() =>
            _context.AppointmentNotificationEmails.AsNoTracking().OrderBy(e => e.Email).ToListAsync();

        public Task<List<string>> GetActiveAddressesAsync() =>
            _context.AppointmentNotificationEmails.Where(e => e.IsActive).Select(e => e.Email).ToListAsync();

        public Task<AppointmentNotificationEmail?> GetByIdAsync(int id) =>
            _context.AppointmentNotificationEmails.FirstOrDefaultAsync(e => e.Id == id);

        public async Task CreateAsync(AppointmentNotificationEmail email)
        {
            _context.AppointmentNotificationEmails.Add(email);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        // The column's collation is case-insensitive, so this also catches
        // the same address typed with different capitals.
        public Task<bool> IsEmailExist(string email, int excludeId) =>
            _context.AppointmentNotificationEmails.AnyAsync(e => e.Email == email && e.Id != excludeId);
    }
}
