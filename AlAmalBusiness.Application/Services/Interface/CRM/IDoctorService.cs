using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface IDoctorService
    {
        Task<DoctorResponse> CreateDoctorAsync(DoctorDTO doctor);
        Task<IEnumerable<DoctorDTO>> GetAllDoctorsAsync();
        Task<IEnumerable<DoctorDTO>> GetActiveDoctorsAsync();
        Task<DoctorResponse> GetDoctorByIdAsync(int doctorId);
        Task<DoctorResponse> UpdateDoctorAsync(int doctorId, DoctorDTO doctor);
    }
}
