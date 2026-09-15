using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.CRM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface ILeadRepo
    {
        // Queued, not saved — the caller saves it together with its Created entry.
        void AddLead(Lead lead);
        // Tracked, for mutation flows. No user rows: only the admin edit asks
        // for the lookup navigations (Doctor/Procedure/Referal), which it
        // needs to describe what changed.
        Task<Lead?> GetLeadByIdAsync(int id, bool includeLookups = false);
        // No-tracking projection for read-only detail responses.
        Task<LeadDetailRow?> GetLeadDetailAsync(int id);
        // One lead as a list row — the LeadCreated push.
        Task<LeadListRow?> GetListRowAsync(int id);
        // Admin recycle bin. Soft-deleted leads are invisible to every other
        // method here (global query filter); these are the only ways to reach
        // them. GetDeletedLeadAsync is tracked (restore).
        Task<Lead?> GetDeletedLeadAsync(int id);
        Task<LeadDetailRow?> GetDeletedLeadDetailAsync(int id);
        Task<DeletedLead?> GetDeletedRecordAsync(int leadId);
        void AddDeletedRecord(DeletedLead record);
        void RemoveDeletedRecord(DeletedLead record);
        Task<(List<DeletedLeadRow> Items, int TotalCount)> GetDeletedPagedAsync(string? search, int page, int pageSize);

        // List queries project straight into LeadListRow (no entity
        // materialization, no Includes) — see LeadListRow.
        // GetAllLeadsAsync is the calendar feed: only leads with at least one
        // logged call, each row carrying its latest call's date/note. from/to
        // (inclusive days) narrow it to leads whose latest call falls inside.
        Task<List<LeadListRow>> GetAllLeadsAsync(bool excludeCompleted = false, DateTime? from = null, DateTime? to = null);
        Task<List<LeadListRow>> GetMineAsync(string userId, bool excludeCompleted = false);
        Task<List<LeadListRow>> GetCreatedByMeAsync(string userId, bool excludeCompleted = false);

        Task<(List<LeadListRow> Items, int TotalCount)> GetPagedAsync(LeadListQuery query);
        Task<(List<LeadListRow> Items, int TotalCount)> GetCreatedByMePagedAsync(string userId, LeadListQuery query);

        Task<int> CountAllAsync();
        Task<Dictionary<LeadStatus, int>> GetStatusCountsAsync(DateTime? from = null, DateTime? to = null);
        Task<Dictionary<int, int>> GetReferralSourceCountsAsync();

        Task<List<(string UserId, string? Username, int Total, int Success, int Closed)>> GetLeadCountsByCreatorAsync();

        // Badge counts for the 5 case-queue tabs, computed directly — does not
        // go through GetPagedAsync/LeadListQuery, so it never touches the
        // GetPaged filter cache (see LeadController.GetQueueCounts).
        Task<(int All, int Today, int Mine, int Unassigned, int Closed)> GetQueueCountsAsync(string userId);

        Task<List<DoctorLeadRow>> GetByDoctorAsync(int doctorId, DateTime? from = null, DateTime? to = null);

        Task<List<(int ProcedureId, int Total, int Pending, int Waiting, int Success, int Closed)>> GetLeadCountsByProcedureAsync(DateTime? from = null, DateTime? to = null);

        Task<List<(int DoctorId, int Total, int Pending, int Waiting, int Success, int Closed, List<(int ProcedureId, int Count)> Procedures)>> GetDoctorStatsWithProceduresAsync(DateTime? from = null, DateTime? to = null);

        // Dashboard KPI support — counts/trend of leads created within [from, toExclusive).
        Task<int> CountCreatedInRangeAsync(DateTime from, DateTime toExclusive);
        Task<Dictionary<DateTime, int>> GetCreatedDailyCountsAsync(DateTime from, DateTime toExclusive);
        Task<List<(string UserId, string? Username, int Total, int Success, int Closed)>> GetCreatedCountsByUserInRangeAsync(DateTime from, DateTime toExclusive);

        Task SaveChangesAsync();
    }
}
