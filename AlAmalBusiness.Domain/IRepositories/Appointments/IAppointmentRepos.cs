using AlAmalBusiness.Domain.Models.Appointments;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Appointments
{
    public interface IAppointmentRequestRepo
    {
        Task<AppointmentRequest> CreateAsync(AppointmentRequest request);

        // Tracked, for the workflow actions to mutate.
        Task<AppointmentRequest?> GetByIdAsync(int id);

        // Detail view — the request plus its department, source and assignee
        // names.
        Task<AppointmentRequest?> GetDetailAsync(int id);

        Task<(List<AppointmentListRow> Items, int TotalCount)> PageRequestsAsync(AppointmentListQuery query);

        // The dashboard's numbers, aggregated in SQL — never a page-through.
        Task<AppointmentStatsRows> GetStatsAsync(AppointmentStatsQuery query);

        Task SaveChangesAsync();
    }

    public interface IAppointmentHistoryRepo
    {
        // Queued only — the caller saves it together with whatever it changed
        // on the request itself, so a timeline entry can never be written
        // without the change it describes.
        void Add(AppointmentHistory history);

        Task<List<AppointmentHistory>> GetByAppointmentAsync(int appointmentId);
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
}
