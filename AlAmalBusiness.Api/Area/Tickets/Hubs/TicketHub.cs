using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.Tickets.Hubs
{
    // Live tickets, for the people who solve them and the people who raised
    // them. LeadHub is a plain broadcast to every connected client; this one
    // is not, and deliberately so: a ticket push carries the patient's MRN
    // and the reason it was raised, and a flagged ticket belongs to the
    // insurance desk alone. So every connection lands in the group(s) its
    // own token earns it, and the notifier sends to a group, never to All.
    //
    // Three kinds of group, because there are three different audiences:
    //   support / insurance — the desk's incoming queue. A new ticket, and
    //     every close or reopen, so one agent's panel drops what another
    //     already did.
    //   user:{id}           — one person's own tickets. A raiser is on the
    //     hub only for this: the moment their ticket is solved they hear
    //     about it, and they receive nothing else on the hub at all.
    //
    // The raising roles still aren't mailed about other people's tickets and
    // still can't see the desk queues — joining a group is decided from the
    // token here, not asked for by the client.
    [Authorize(Roles = Access)]
    public class TicketHub : Hub
    {
        // Raising roles are on the hub too since 2026-09-22 — for their own
        // tickets only (see OnConnectedAsync). This mirrors the console's
        // /api/tickets/realtime gate, which hands out the token.
        public const string Access =
            nameof(AppRoles.TSupport) + "," + nameof(AppRoles.TInsurance) + "," +
            nameof(AppRoles.TManager) + "," + nameof(AppRoles.TEmployee) + "," + nameof(AppRoles.Admin);

        // The desk a connection listens to. An Admin joins both, which is
        // why these are groups and not one connection-level flag.
        public const string SupportGroup = "tickets:support";
        public const string InsuranceGroup = "tickets:insurance";

        // One person's own group. Every connection that has a user id joins
        // it, an agent's included: an agent who raised a ticket is told when
        // somebody else solves it, exactly as the email already tells them.
        public static string UserGroup(string userId) => "tickets:user:" + userId;

        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            var isAdmin = user?.IsInRole(nameof(AppRoles.Admin)) == true;

            if (isAdmin || user?.IsInRole(nameof(AppRoles.TSupport)) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, SupportGroup);

            if (isAdmin || user?.IsInRole(nameof(AppRoles.TInsurance)) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, InsuranceGroup);

            // `sub` on the token, mapped to NameIdentifier the same way every
            // controller reads it.
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            await base.OnConnectedAsync();
        }
        // No OnDisconnectedAsync: SignalR drops a gone connection from every
        // group it holds by itself.
    }
}
