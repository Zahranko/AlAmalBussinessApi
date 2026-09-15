using AlAmalBusiness.Domain.Models.Appointments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Appointments
{
    public interface IAppointmentRequestRepo
    {
        Task<AppointmentRequest> CreateAsync(AppointmentRequest request);
    }

    public interface IAppointmentProcedureRepo
    {
        Task<List<AppointmentProcedure>> GetAllAsync();
        Task<List<AppointmentProcedure>> GetActiveAsync();
        Task<AppointmentProcedure?> GetByIdAsync(int id);
        Task CreateAsync(AppointmentProcedure procedure);
        Task SaveChangesAsync();
        Task<bool> IsNameExist(string name, int excludeId);
    }

    public interface IAppointmentReferralSourceRepo
    {
        Task<List<AppointmentReferralSource>> GetAllAsync();
        Task<List<AppointmentReferralSource>> GetActiveAsync();
        Task<AppointmentReferralSource?> GetByIdAsync(int id);
        Task CreateAsync(AppointmentReferralSource source);
        Task SaveChangesAsync();
        Task<bool> IsNameExist(string name, int excludeId);
    }

    public interface IAppointmentEmailRepo
    {
        Task<List<AppointmentNotificationEmail>> GetAllAsync();
        // Just the addresses of the active rows — all the notifier needs.
        Task<List<string>> GetActiveAddressesAsync();
        Task<AppointmentNotificationEmail?> GetByIdAsync(int id);
        Task CreateAsync(AppointmentNotificationEmail email);
        Task SaveChangesAsync();
        Task<bool> IsEmailExist(string email, int excludeId);
    }
}
