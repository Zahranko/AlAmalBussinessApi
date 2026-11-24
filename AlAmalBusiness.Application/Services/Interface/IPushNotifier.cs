using AlAmalBusiness.Application.DTOs.Push;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.Services.Interface
{
    // Phone push notifications (added 2026-11-22 for the mobile app),
    // implemented in Api over Firebase Cloud Messaging so Application carries
    // no Firebase package — the IEmailQueue arrangement. Every method only
    // enqueues: it never blocks, never touches the database or the network,
    // and never throws, so a push can't slow down or fail the write that
    // triggered it. Recipients are resolved later, on the background sender,
    // with the same rules as the matching email.
    public interface IPushNotifier
    {
        bool IsEnabled { get; }

        void ToUsers(IReadOnlyCollection<string> userIds, PushMessage message);

        // Active holders of the role in that department (home or granted).
        void ToRoleInDepartment(string role, int departmentId, PushMessage message);

        // Active holders of any of the roles, minus the one who acted.
        void ToRoles(IReadOnlyCollection<string> roles, string? excludeUserId, PushMessage message);
    }
}
