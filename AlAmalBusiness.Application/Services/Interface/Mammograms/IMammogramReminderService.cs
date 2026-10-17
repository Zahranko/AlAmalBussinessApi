using System.Threading;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.Mammograms
{
    public interface IMammogramReminderService
    {
        // Sends the reminder template to every register row whose follow-up
        // date is today and that hasn't been reminded yet.
        Task<MammogramReminderRunResult> SendDueAsync(CancellationToken ct = default);

        // Reads the latest delivery status of recent reminders back into
        // their rows.
        Task<int> SyncStatusesAsync(CancellationToken ct = default);
    }

    public class MammogramReminderRunResult
    {
        public bool Enabled { get; set; }
        public int Due { get; set; }
        public int Queued { get; set; }
        public int Failed { get; set; }
        // Not sent, left NotSent for the next run: the platform couldn't be
        // reached, or refused the whole bulk send (Error says why).
        public int Deferred { get; set; }
        public int StatusesUpdated { get; set; }
        public string? Error { get; set; }
        // The platform's campaign the run became, under its Bulk send.
        public int? CampaignId { get; set; }
    }
}
