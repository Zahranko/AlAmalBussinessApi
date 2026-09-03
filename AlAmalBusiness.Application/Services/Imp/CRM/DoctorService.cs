using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.CRM
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepo _repo;

        public DoctorService(IDoctorRepo repo)
        {
            _repo = repo;
        }

        public async Task<DoctorResponse> CreateDoctorAsync(DoctorDTO doctor)
        {
            var exists = await _repo.IsNameExist(doctor.Name!, 0);
            if (exists)
            {
                return new DoctorResponse { Success = false, Message = $"Doctor with name '{doctor.Name}' already exists." };
            }

            var entity = new Doctors { Name = doctor.Name };
            await _repo.CreateAsync(entity);

            return new DoctorResponse
            {
                Success = true,
                Doctor = new DoctorDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }

        public async Task<IEnumerable<DoctorDTO>> GetAllDoctorsAsync()
        {
            var doctors = await _repo.GetAllAsync();
            return doctors.Select(d => new DoctorDTO { Id = d.Id, Name = d.Name, IsActive = d.IsActive });
        }

        public async Task<IEnumerable<DoctorDTO>> GetActiveDoctorsAsync()
        {
            var doctors = await _repo.GetActiveAsync();
            return doctors.Select(d => new DoctorDTO { Id = d.Id, Name = d.Name, IsActive = d.IsActive });
        }

        public async Task<DoctorResponse> GetDoctorByIdAsync(int doctorId)
        {
            var doctor = await _repo.GetByIdAsync(doctorId);
            if (doctor == null)
            {
                return new DoctorResponse { Success = false, Message = $"Doctor with ID {doctorId} not found." };
            }

            return new DoctorResponse
            {
                Success = true,
                Doctor = new DoctorDTO { Id = doctor.Id, Name = doctor.Name, IsActive = doctor.IsActive }
            };
        }

        public async Task<DoctorResponse> UpdateDoctorAsync(int doctorId, DoctorDTO doctor)
        {
            var entity = await _repo.GetByIdAsync(doctorId);
            if (entity == null)
            {
                return new DoctorResponse { Success = false, Message = $"Doctor with ID {doctorId} not found." };
            }
            if (string.IsNullOrWhiteSpace(doctor.Name))
            {
                return new DoctorResponse { Success = false, Message = "Doctor name is empty" };
            }

            var nameExists = await _repo.IsNameExist(doctor.Name, doctorId);
            if (nameExists)
            {
                return new DoctorResponse { Success = false, Message = $"Doctor with name '{doctor.Name}' already exists." };
            }

            entity.Name = doctor.Name;
            entity.IsActive = doctor.IsActive;
            await _repo.UpdateAsync(entity);

            return new DoctorResponse
            {
                Success = true,
                Doctor = new DoctorDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }
    }
}
