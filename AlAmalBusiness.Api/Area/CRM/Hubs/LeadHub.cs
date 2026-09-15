using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AlAmalBusiness.Api.Area.CRM.Hubs
{
    // Plain broadcast hub — no groups, no per-user targeting. Connected
    // dashboards get LeadCreated/LeadStatusChanged pushes so they live-update.
    // CRM roles only: LeadCreated carries the patient's name and phone, which
    // feedback and questionnaire staff have no business receiving.
    [Authorize(Roles = CrmRoles.Access)]
    public class LeadHub : Hub
    {
    }
}
