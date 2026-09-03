using AlAmalBusiness.Application.DTOs.CRM.Lead.Response;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CRM
{
    // Plain broadcast — no persisted inbox, no per-user targeting. Implemented
    // in the Api project (over SignalR) so Application stays free of any
    // real-time-transport package reference.
    public interface ILeadNotifier
    {
        Task LeadCreatedAsync(LeadListItemResponse lead);
        Task LeadStatusChangedAsync(int leadId, string status);
    }
}
