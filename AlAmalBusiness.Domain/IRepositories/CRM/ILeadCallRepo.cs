using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface ILeadCallRepo
    {
        void Add(LeadCall call);

        // Tracked — for mutation flow (mark done).
        Task<LeadCall?> GetByIdAsync(int id);

        // No-tracking, ordered by Date — for detail response.
        Task<List<LeadCall>> GetByLeadAsync(int leadId);

        Task<int> CountByLeadAsync(int leadId);

        // The most recently added call (by CreatedAt) per lead, for every id
        // in leadIds — used to enrich the bulk lead list for the case
        // calendar without an N+1 detail fetch per lead. Leads with no calls
        // are simply absent from the result.
        Task<Dictionary<int, LeadCall>> GetLastCallsByLeadIdsAsync(IEnumerable<int> leadIds);
    }
}
