using AlAmalBusiness.Application.DTOs.Tickets.Response;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Hubs
{
    public class SignalRTicketNotifier : ITicketNotifier
    {
        private readonly IHubContext<TicketHub> _hubContext;

        public SignalRTicketNotifier(IHubContext<TicketHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task TicketCreatedAsync(TicketPushResponse ticket) =>
            _hubContext.Clients.Group(GroupFor(ticket.IsInsurance)).SendAsync("TicketCreated", ticket);

        public Task TicketChangedAsync(int ticketId, bool isInsurance, string status, string? byName) =>
            _hubContext.Clients.Group(GroupFor(isInsurance))
                .SendAsync("TicketChanged", new { ticketId, status, byName });

        // The flag decides the desk, here as everywhere else in this feature.
        // An Admin is in both groups, so they receive either way.
        private static string GroupFor(bool isInsurance) =>
            isInsurance ? TicketHub.InsuranceGroup : TicketHub.SupportGroup;
    }
}
