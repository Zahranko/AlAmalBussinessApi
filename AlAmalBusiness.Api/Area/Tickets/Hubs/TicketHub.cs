using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Hubs
{
    // Live tickets for the people who solve them. LeadHub is a plain
    // broadcast to every connected client; this one is not, and deliberately
    // so: a ticket push carries the patient's MRN and the reason it was
    // raised, and a flagged ticket belongs to the insurance desk alone. So
    // every connection lands in the group(s) for the desk(s) its token says
    // it works, and the notifier sends to a group rather than to All.
    //
    // The raising roles (TManager/TEmployee) are not on this hub at all —
    // they are not mailed about each ticket either, for the same reason: a
    // raiser follows their own ticket in the console, they are not on call
    // for the queue.
    [Authorize(Roles = Access)]
    public class TicketHub : Hub
    {
        public const string Access =
            nameof(AppRoles.TSupport) + "," + nameof(AppRoles.TInsurance) + "," + nameof(AppRoles.Admin);

        // The desk a connection listens to. An Admin joins both, which is
        // why these are groups and not one connection-level flag.
        public const string SupportGroup = "tickets:support";
        public const string InsuranceGroup = "tickets:insurance";

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var isAdmin = user?.IsInRole(nameof(AppRoles.Admin)) == true;

            if (isAdmin || user?.IsInRole(nameof(AppRoles.TSupport)) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroup);

            if (isAdmin || user?.IsInRole(nameof(AppRoles.TInsurance)) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, InsuranceGroup);

            await base.OnConnectedAsync();
        }
        // No OnDisconnectedAsync: SignalR drops a gone connection from every
        // group it holds by itself.
    }
}
