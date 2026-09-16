using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Appointments
{
    // The "how did you hear about us" list behind the public appointment
    // page. Same rules as the CRM lookup lists (ProcedureService and
    // friends): a unique name, IsActive in the same body, no delete —
    // retiring an entry keeps it on the requests that already picked it.
    //
    // The procedures and notification-emails lists that used to live here
    // were removed on 2026-09-16 (see AppointmentRequest).
    public class AppointmentListService : IAppointmentListService
    {
        private readonly IAppointmentReferralSourceRepo _referralSources;

        public AppointmentListService(IAppointmentReferralSourceRepo referralSources)
        {
            _referralSources = referralSources;
        }

        public async Task<List<AppointmentListItemDTO>> GetReferralSourcesAsync() =>
            (await _referralSources.GetAllAsync()).Select(r => ToDto(r.Id, r.Name, r.IsActive)).ToList();

        public async Task<AppointmentListResponse<AppointmentListItemDTO>> CreateReferralSourceAsync(AppointmentListItemDTO dto)
        {
            var name = Clean(dto.Name);
            if (name == null) return Failed<AppointmentListItemDTO>("Referral source name is empty.");
            if (await _referralSources.IsNameExist(name, 0))
                return Failed<AppointmentListItemDTO>($"Referral source with name '{name}' already exists.");

            var entity = new AppointmentReferralSource { Name = name, IsActive = dto.IsActive };
            await _referralSources.CreateAsync(entity);
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        public async Task<AppointmentListResponse<AppointmentListItemDTO>> UpdateReferralSourceAsync(int id, AppointmentListItemDTO dto)
        {
            var entity = await _referralSources.GetByIdAsync(id);
            if (entity == null) return Missing<AppointmentListItemDTO>($"Referral source with ID {id} not found.");

            var name = Clean(dto.Name);
            if (name == null) return Failed<AppointmentListItemDTO>("Referral source name is empty.");
            if (await _referralSources.IsNameExist(name, id))
                return Failed<AppointmentListItemDTO>($"Referral source with name '{name}' already exists.");

            entity.Name = name;
            entity.IsActive = dto.IsActive;
            await _referralSources.SaveChangesAsync();
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        // ---------- helpers ----------

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static AppointmentListItemDTO ToDto(int id, string name, bool isActive) =>
            new() { Id = id, Name = name, IsActive = isActive };

        private static AppointmentListResponse<T> Ok<T>(T item) => new() { Success = true, Item = item };

        private static AppointmentListResponse<T> Failed<T>(string message) => new() { Success = false, Message = message };

        private static AppointmentListResponse<T> Missing<T>(string message) =>
            new() { Success = false, NotFound = true, Message = message };
    }
}
