using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface IReferalSourceService
    {
        Task<ReferalSourceResponse> CreateReferalSourceAsync(ReferalSourceDTO referalSource);
        Task<IEnumerable<ReferalSourceDTO>> GetAllReferalSourcesAsync();
        Task<IEnumerable<ReferalSourceDTO>> GetActiveReferalSourcesAsync();
        Task<ReferalSourceResponse> GetReferalSourceByIdAsync(int referalSourceId);
        Task<ReferalSourceResponse> UpdateReferalSourceAsync(int referalSourceId, ReferalSourceDTO referalSource);
    }
}
