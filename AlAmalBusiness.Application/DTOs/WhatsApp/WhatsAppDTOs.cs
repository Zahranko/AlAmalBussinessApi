using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.WhatsApp
{
    // A bulk send through the hospital's WhatsApp platform
    // (whatsappapi.alamalhospitaljo.com, its /api/v1/campaigns): one campaign
    // there (it shows under Bulk send), one approved template, a message per
    // recipient. The platform saves each number as a customer under
    // Name/FileNumber. Ref is the caller's own id for the recipient, echoed
    // back on its result line.
    public class WhatsAppBulkMessage
    {
        public string Name { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string? Language { get; set; }
        public List<WhatsAppBulkRecipient> Recipients { get; set; } = new();
    }

    public class WhatsAppBulkRecipient
    {
        public string Ref { get; set; } = string.Empty;
        // Digits only with the country code, "962799832795".
        public string Phone { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? FileNumber { get; set; }
        public List<string> Body { get; set; } = new();
    }

    // Success/Error/Retryable describe the request as a whole; when it
    // succeeded, Results holds one line per recipient — a message id, or why
    // that one wasn't queued.
    public class WhatsAppBulkResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        // Nothing reached the platform (it was down, or rate-limited us): safe
        // to send the same list again later. False when the platform refused
        // it, or when it may have queued it before the answer was lost.
        public bool Retryable { get; set; }
        public int? CampaignId { get; set; }
        public List<WhatsAppBulkLine> Results { get; set; } = new();
    }

    public class WhatsAppBulkLine
    {
        public string? Ref { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }

    // The platform's own status words: queued, sending, sent, delivered,
    // read, failed, cancelled.
    public class WhatsAppMessageStatus
    {
        public string Status { get; set; } = string.Empty;
        public string? Error { get; set; }
    }
}
