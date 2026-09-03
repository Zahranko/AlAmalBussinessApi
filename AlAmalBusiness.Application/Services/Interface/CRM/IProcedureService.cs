using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface IProcedureService
    {
        Task<ProcedureResponse> CreateProcedureAsync(ProcedureDTO procedure);
        Task<IEnumerable<ProcedureDTO>> GetAllProceduresAsync();
        Task<IEnumerable<ProcedureDTO>> GetActiveProceduresAsync();
        Task<ProcedureResponse> GetProcedureByIdAsync(int procedureId);
        Task<ProcedureResponse> UpdateProcedureAsync(int procedureId, ProcedureDTO procedure);
    }
}
