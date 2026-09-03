using AlAmalBusiness.Domain.Models.CRM;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface ILeadHistoryRepo
    {
        void Add(LeadHistory history);
        Task<List<LeadHistory>> GetByLeadAsync(int leadId);
        Task<List<LeadHistory>> GetFollowUpsByLeadIdsAsync(IEnumerable<int> leadIds);

        // Dashboard KPI support — a lead reaching Success within [from, toExclusive),
        // counted per follow-up event (a reopened-then-re-succeeded lead counts twice).
        Task<int> CountSucceededInRangeAsync(DateTime from, DateTime toExclusive);
        Task<Dictionary<DateTime, int>> GetSucceededDailyCountsAsync(DateTime from, DateTime toExclusive);
    }
}
