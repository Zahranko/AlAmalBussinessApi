using AlAmalBusiness.Application.DTOs.WhatsApp;
using System.Threading;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface
{
    // Application-side abstraction over the hospital's WhatsApp platform,
    // implemented in Api (WhatsAppPlatformClient) so Application holds no
    // HTTP plumbing — the same arrangement as IEmailQueue.
    public interface IWhatsAppSender
    {
        // False when no API key is configured: nothing would be accepted.
        bool IsEnabled { get; }

        Task<WhatsAppBulkResult> SendBulkAsync(WhatsAppBulkMessage message, CancellationToken ct = default);

        // Null when the platform doesn't know the id or couldn't be reached;
        // the caller just asks again next time.
        Task<WhatsAppMessageStatus?> GetStatusAsync(string messageId, CancellationToken ct = default);
    }
}
