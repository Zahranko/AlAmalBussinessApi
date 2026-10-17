using AlAmalBusiness.Application.Services.Interface;
using AlAmalBusiness.Application.Services.Interface.Mammograms;
using AlAmalBusiness.Domain.Constants;

namespace AlAmalBusiness.Api.Area.Mammograms
{
    // Sends the register's WhatsApp follow-up reminders on the follow-up day
    // and reads their delivery status back. A hosted service in the web
    // process, the QuestionnaireMonthlyReportScheduler shape: the shared host
    // runs no scheduled jobs, and the console's keep-alive ping keeps it awake.
    //
    // Sends once a day: the first wake at or after SendHour (08:00 Jordan)
    // sends everything due today as one bulk send. If the platform couldn't
    // take it, the next wakes try again, but never at or after LastHour, so
    // nobody is messaged at night. A row added or corrected to today after
    // that day's run waits for the Follow-ups page's "Send now" (or a restart
    // of the app, which forgets that today already ran — harmless, since only
    // rows not yet reminded are ever sent). A row whose day passed while the
    // app was down is not sent late. Statuses are read back on every wake.
    // Exactly-once is MammogramReminderService's claim, not this timer.
    public sealed class MammogramReminderScheduler : BackgroundService
    {
        private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<MammogramReminderScheduler> _logger;
        private readonly bool _enabled;
        private readonly int _sendHour;
        private readonly int _lastHour;
        private DateOnly? _sentOn;

        public MammogramReminderScheduler(IServiceScopeFactory scopes, IConfiguration config, ILogger<MammogramReminderScheduler> logger)
        {
            _scopes = scopes;
            _logger = logger;
            var section = config.GetSection("MammogramReminders");
            _enabled = section.GetValue("Enabled", true);
            _sendHour = Math.Clamp(section.GetValue("SendHour", 8), 0, 23);
            _lastHour = Math.Clamp(section.GetValue("LastHour", 20), _sendHour + 1, 24);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Mammogram WhatsApp reminders are disabled (MammogramReminders:Enabled).");
                return;
            }

            using (var scope = _scopes.CreateScope())
            {
                if (!scope.ServiceProvider.GetRequiredService<IWhatsAppSender>().IsEnabled)
                {
                    _logger.LogInformation("Mammogram WhatsApp reminders are off: WhatsApp:ApiKey is not set.");
                    return;
                }
            }

            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await RunAsync(stoppingToken);
                    await Task.Delay(CheckEvery, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App shutting down.
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var reminders = scope.ServiceProvider.GetRequiredService<IMammogramReminderService>();

                var now = AppClock.Now;
                var today = DateOnly.FromDateTime(now);
                if (_sentOn != today && now.Hour >= _sendHour && now.Hour < _lastHour)
                {
                    var result = await reminders.SendDueAsync(ct);
                    if (result.Deferred == 0) _sentOn = today;
                }
                await reminders.SyncStatusesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Never let one bad run kill the loop; the next wake retries.
                _logger.LogError(ex, "Mammogram WhatsApp reminder run failed.");
            }
        }
    }
}
