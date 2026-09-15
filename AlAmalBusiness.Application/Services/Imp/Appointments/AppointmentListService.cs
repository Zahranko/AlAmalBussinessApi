using AlAmalBusiness.Application.DTOs.Appointments;
using AlAmalBusiness.Application.Services.Interface.Appointments;
using AlAmalBusiness.Domain.IRepositories.Appointments;
using AlAmalBusiness.Domain.Models.Appointments;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Appointments
{
    // The three admin-maintained lists behind the public appointment page.
    // Same rules as the CRM lookup lists (ProcedureService and friends): a
    // unique name, IsActive in the same body, no delete — retiring an entry
    // keeps it on the requests that already picked it.
    public class AppointmentListService : IAppointmentListService
    {
        private readonly IAppointmentProcedureRepo _procedures;
        private readonly IAppointmentReferralSourceRepo _referralSources;
        private readonly IAppointmentEmailRepo _emails;

        public AppointmentListService(
            IAppointmentProcedureRepo procedures,
            IAppointmentReferralSourceRepo referralSources,
            IAppointmentEmailRepo emails)
        {
            _procedures = procedures;
            _referralSources = referralSources;
            _emails = emails;
        }

        // ---------- procedures ----------

        public async Task<List<AppointmentListItemDTO>> GetProceduresAsync() =>
            (await _procedures.GetAllAsync()).Select(p => ToDto(p.Id, p.Name, p.IsActive)).ToList();

        public async Task<AppointmentListResponse<AppointmentListItemDTO>> CreateProcedureAsync(AppointmentListItemDTO dto)
        {
            var name = Clean(dto.Name);
            if (name == null) return Failed<AppointmentListItemDTO>("Procedure name is empty.");
            if (await _procedures.IsNameExist(name, 0))
                return Failed<AppointmentListItemDTO>($"Procedure with name '{name}' already exists.");

            var entity = new AppointmentProcedure { Name = name, IsActive = dto.IsActive };
            await _procedures.CreateAsync(entity);
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        public async Task<AppointmentListResponse<AppointmentListItemDTO>> UpdateProcedureAsync(int id, AppointmentListItemDTO dto)
        {
            var entity = await _procedures.GetByIdAsync(id);
            if (entity == null) return Missing<AppointmentListItemDTO>($"Procedure with ID {id} not found.");

            var name = Clean(dto.Name);
            if (name == null) return Failed<AppointmentListItemDTO>("Procedure name is empty.");
            if (await _procedures.IsNameExist(name, id))
                return Failed<AppointmentListItemDTO>($"Procedure with name '{name}' already exists.");

            entity.Name = name;
            entity.IsActive = dto.IsActive;
            await _procedures.SaveChangesAsync();
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        // ---------- referral sources ----------

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

        // ---------- notification emails ----------

        public async Task<List<AppointmentEmailDTO>> GetEmailsAsync() =>
            (await _emails.GetAllAsync()).Select(ToDto).ToList();

        public async Task<AppointmentListResponse<AppointmentEmailDTO>> CreateEmailAsync(AppointmentEmailDTO dto)
        {
            if (!TryNormalizeEmail(dto.Email, out var address))
                return Failed<AppointmentEmailDTO>("Email address is not valid.");
            if (await _emails.IsEmailExist(address, 0))
                return Failed<AppointmentEmailDTO>($"'{address}' is already on the list.");

            var entity = new AppointmentNotificationEmail { Email = address, Name = Clean(dto.Name), IsActive = dto.IsActive };
            await _emails.CreateAsync(entity);
            return Ok(ToDto(entity));
        }

        public async Task<AppointmentListResponse<AppointmentEmailDTO>> UpdateEmailAsync(int id, AppointmentEmailDTO dto)
        {
            var entity = await _emails.GetByIdAsync(id);
            if (entity == null) return Missing<AppointmentEmailDTO>($"Email with ID {id} not found.");

            if (!TryNormalizeEmail(dto.Email, out var address))
                return Failed<AppointmentEmailDTO>("Email address is not valid.");
            if (await _emails.IsEmailExist(address, id))
                return Failed<AppointmentEmailDTO>($"'{address}' is already on the list.");

            entity.Email = address;
            entity.Name = Clean(dto.Name);
            entity.IsActive = dto.IsActive;
            await _emails.SaveChangesAsync();
            return Ok(ToDto(entity));
        }

        // ---------- helpers ----------

        // Same acceptance rule as UserServices.TryNormalizeEmail, except an
        // address is required here — the row is nothing but the address.
        private static bool TryNormalizeEmail(string? value, out string email)
        {
            email = string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var trimmed = value.Trim();
            if (trimmed.Length > 256
                || !System.Net.Mail.MailAddress.TryCreate(trimmed, out var parsed)
                || parsed.Address != trimmed)
                return false;

            email = trimmed;
            return true;
        }

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static AppointmentListItemDTO ToDto(int id, string name, bool isActive) =>
            new() { Id = id, Name = name, IsActive = isActive };

        private static AppointmentEmailDTO ToDto(AppointmentNotificationEmail e) =>
            new() { Id = e.Id, Email = e.Email, Name = e.Name, IsActive = e.IsActive };

        private static AppointmentListResponse<T> Ok<T>(T item) => new() { Success = true, Item = item };

        private static AppointmentListResponse<T> Failed<T>(string message) => new() { Success = false, Message = message };

        private static AppointmentListResponse<T> Missing<T>(string message) =>
            new() { Success = false, NotFound = true, Message = message };
    }
}
