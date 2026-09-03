using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.DTOs.CRM.Lead;
using AlAmalBusiness.Application.DTOs.CRM.Lead.Response;
using AlAmalBusiness.Application.DTOs.CRM.Stats;
using AlAmalBusiness.Domain.IRepositories.CRM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    public interface ILeadService
    {
        Task<CreateLeadResponse> CreateLeadAsync(CreateLeadDTO lead, string currentUserId);
        Task<LeadDetailResponse?> GetLeadDetailAsync(int id);
        Task DeleteLeadAsync(int id);

        Task<List<LeadListItemResponse>> GetAllLeadsAsync(bool excludeCompleted = false);
        Task<List<LeadListItemResponse>> GetMineAsync(string userId, bool excludeCompleted = false);
        Task<List<LeadListItemResponse>> GetCreatedByMeAsync(string userId, bool excludeCompleted = false);
        Task<PagedResultDTO<LeadListItemResponse>> GetPagedAsync(LeadListQuery query);
        Task<PagedResultDTO<LeadListItemResponse>> GetCreatedByMePagedAsync(string userId, LeadListQuery query);

        Task<LeadActionResponse> ClaimLeadAsync(int id, string userId);
        Task<LeadActionResponse> FollowUpAsync(int id, string userId, FollowUpLeadDTO request);
        Task<LeadActionResponse> ReopenAsync(int id, string adminUserId);
        Task<LeadActionResponse> AdminUpdateLeadAsync(int id, string adminUserId, AdminUpdateLeadDTO request);

        // Up to 6 calls per lead. The first call logged on a New lead moves it
        // to Pending; any other status is left as-is.
        Task<LeadActionResponse> LogCallAsync(int id, string userId, LeadCallDTO request);
        Task<LeadActionResponse> MarkCallDoneAsync(int id, int callId, string userId);

        Task<List<AssignableUserResponse>> GetActiveUsersAsync();

        Task<DashboardKpiDTO> GetDashboardKpisAsync();
        Task<List<EmployeeCaseCountDTO>> GetEmployeeCaseCountsAsync(string period);
        Task<LeadStatusBandDTO> GetStatusBandAsync();
        Task<List<ReferralSourceStatDTO>> GetLeadSourcesAsync();

        Task<AdminStatsDTO> GetStatsAsync();
        Task<HospitalManagerStatsDTO> GetHospitalManagerStatsAsync(DateTime? from, DateTime? to);
        Task<DoctorLeadExportDTO?> GetDoctorLeadExportAsync(int doctorId, DateTime? from, DateTime? to);
    }
}
