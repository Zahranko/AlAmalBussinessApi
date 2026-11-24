using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Push
{
    // One push notification. Title/Body are what shows on the lock screen,
    // so they carry no patient name, phone or message text — only the kind
    // of thing that arrived and where it went (a reference, a department).
    // The app opens the full record, behind sign-in, from Kind + Id.
    public class PushMessage
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // "feedback" | "ticket" | "maintenance" | "lead-edit"
        public string Kind { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;

        // Same thread on the phone for the same record (Android tag / iOS
        // thread-id), so five updates to one ticket stack as one group.
        public string? Thread { get; set; }

        public Dictionary<string, string> Extra { get; set; } = new();
    }
}
