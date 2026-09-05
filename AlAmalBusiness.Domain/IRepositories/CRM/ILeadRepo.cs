using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.CRM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface ILeadRepo
    {
        Task<Lead> CreateLeadAsync(Lead lead);
        Task DeleteLeadAsync(Lead lead);

        // Tracked (with includes) — for mutation flows (claim/follow-up/admin-update).
        Task<Lead?> GetLeadByIdAsync(int id);
        // No-tracking (with includes) — for read-only detail responses.
        Task<Lead?> GetLeadDetailAsync(int id);

        Task<List<Lead>> GetAllLeadsAsync(bool excludeCompleted = false);
        Task<List<Lead>> GetMineAsync(string userId, bool excludeCompleted = false);
        Task<List<Lead>> GetCreatedByMeAsync(string userId, bool excludeCompleted = false);

        Task<(List<Lead> Items, int TotalCount)> GetPagedAsync(LeadListQuery query);
        Task<(List<Lead> Items, int TotalCount)> GetCreatedByMePagedAsync(string userId, LeadListQuery query);

        Task<int> CountAllAsync();
        Task<Dictionary<LeadStatus, int>> GetStatusCountsAsync(DateTime? from = null, DateTime? to = null);
        Task<Dictionary<int, int>> GetReferralSourceCountsAsync();

        Task<List<(string UserId, string? Username, int Total, int Success, int Closed)>> GetLeadCountsByCreatorAsync();

        // Badge counts for the 5 case-queue tabs, computed directly — does not
        // go through GetPagedAsync/LeadListQuery, so it never touches the
        // GetPaged filter cache (see LeadController.GetQueueCounts).
        Task<(int All, int Today, int Mine, int Unassigned, int Closed)> GetQueueCountsAsync(string userId);

        Task<List<Lead>> GetByDoctorAsync(int doctorId, DateTime? from = null, DateTime? to = null);

        Task<List<(int ProcedureId, int Total, int Pending, int Waiting, int Success, int Closed)>> GetLeadCountsByProcedureAsync(DateTime? from = null, DateTime? to = null);

        Task<List<(int DoctorId, int Total, int Pending, int Waiting, int Success, int Closed, List<(int ProcedureId, int Count)> Procedures)>> GetDoctorStatsWithProceduresAsync(DateTime? from = null, DateTime? to = null);

        // Dashboard KPI support — counts/trend of leads created within [from, toExclusive).
        Task<int> CountCreatedInRangeAsync(DateTime from, DateTime toExclusive);
        Task<Dictionary<DateTime, int>> GetCreatedDailyCountsAsync(DateTime from, DateTime toExclusive);
        Task<List<(string UserId, string? Username, int Count)>> GetCreatedCountsByUserInRangeAsync(DateTime from, DateTime toExclusive);

        Task SaveChangesAsync();
    }
}
