using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface IClosedReasonService
    {
        Task<ClosedReasonResponse> CreateClosedReasonAsync(ClosedReasonDTO closedReason);
        Task<IEnumerable<ClosedReasonDTO>> GetAllClosedReasonsAsync();
        Task<IEnumerable<ClosedReasonDTO>> GetActiveClosedReasonsAsync();
        Task<ClosedReasonResponse> GetClosedReasonByIdAsync(int closedReasonId);
        Task<ClosedReasonResponse> UpdateClosedReasonAsync(int closedReasonId, ClosedReasonDTO closedReason);
    }
}
