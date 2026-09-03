using AlAmalBusiness.Application.DTOs.CRM.Lead.Response;
using AlAmalBusiness.Application.Services.Interface.CRM;
using Microsoft.AspNetCore.SignalR;

namespace AlAmalBusiness.Api.Area.CRM.Hubs
{
    public class SignalRLeadNotifier : ILeadNotifier
    {
        private readonly IHubContext<LeadHub> _hubContext;

        public SignalRLeadNotifier(IHubContext<LeadHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task LeadCreatedAsync(LeadListItemResponse lead) =>
            _hubContext.Clients.All.SendAsync("LeadCreated", lead);

        public Task LeadStatusChangedAsync(int leadId, string status) =>
            _hubContext.Clients.All.SendAsync("LeadStatusChanged", new { leadId, status });
    }
}
