using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.LeadEditRequest;
using AlAmalBusiness.Domain.Constants;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public record LeadEditActor(string UserId, bool IsAdmin);

    public interface ILeadEditRequestService
    {
        Task<LeadEditRequestResponse> CreateAsync(int leadId, CreateLeadEditRequestDTO request, LeadEditActor actor);
        Task<PagedResultDTO<LeadEditRequestResponse>> GetPagedAsync(
            LeadEditRequestStatus? status, bool mineOnly, int page, int pageSize, LeadEditActor actor);
        Task<LeadEditRequestResponse?> GetAsync(int id, LeadEditActor actor);
        Task<int> CountPendingAsync();
        Task<LeadEditRequestResponse> ApproveAsync(int id, string? note, LeadEditActor admin);
        Task<LeadEditRequestResponse> DeclineAsync(int id, string? note, LeadEditActor admin);
    }

}
