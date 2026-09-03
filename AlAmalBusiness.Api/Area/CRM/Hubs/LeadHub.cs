using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AlAmalBusiness.Api.Area.CRM.Hubs
{
    // Plain broadcast hub — no groups, no per-user targeting. Connected
    // dashboards get LeadCreated/LeadStatusChanged pushes so they live-update.
    [Authorize]
    public class LeadHub : Hub
    {
    }
}
