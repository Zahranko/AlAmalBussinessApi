using AlAmalBusiness.Application.Services.Interface.Questionnaires;

namespace AlAmalBusiness.Api.Area.Questionnaires
{
    // Sends last month's questionnaire report to every department's QManagers,
    // once, early each month. A hosted service inside the web process — the
    // shared host runs no scheduled jobs — kept awake by the console's
    // 5-minute keep-alive ping.
    //
    // It simply wakes every 30 minutes and asks "is it past SendDay/SendHour,
    // and has last month not gone out yet?". The once-per-month guarantee is
    // the QuestionnaireReportRuns row (unique Year/Month), not this timer, so
    // restarts, recycles and a missed hour all just mean it catches up on the
    // next wake.
    public sealed class QuestionnaireMonthlyReportScheduler : BackgroundService
    {
        private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(30);
        // Let the app finish starting (and the pool warm up) before the first check.
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(2);

        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<QuestionnaireMonthlyReportScheduler> _logger;
        private readonly bool _enabled;
        private readonly int _sendDay;
        private readonly int _sendHour;

        public QuestionnaireMonthlyReportScheduler(
            IServiceScopeFactory scopes,
            IConfiguration config,
            ILogger<QuestionnaireMonthlyReportScheduler> logger)
        {
            _scopes = scopes;
            _logger = logger;
            var section = config.GetSection("QuestionnaireReport");
            _enabled = section.GetValue("Enabled", true);
            _sendDay = Math.Clamp(section.GetValue("SendDay", 1), 1, 28);
            _sendHour = Math.Clamp(section.GetValue("SendHour", 8), 0, 23);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Monthly questionnaire report is disabled (QuestionnaireReport:Enabled).");
                return;
            }

            try
            {
                await Task.Delay(StartupDelay, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await CheckAsync();
                    await Task.Delay(CheckEvery, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App shutting down.
            }
        }

        private async Task CheckAsync()
        {
            // Local time, like every CreatedDate the report counts.
            var now = DateTime.Now;
            if (now.Day < _sendDay || (now.Day == _sendDay && now.Hour < _sendHour))
                return;

            var target = new DateTime(now.Year, now.Month, 1).AddMonths(-1);

            try
            {
                using var scope = _scopes.CreateScope();
                var reports = scope.ServiceProvider.GetRequiredService<IQuestionnaireMonthlyReportService>();
                var result = await reports.SendScheduledAsync(target.Year, target.Month);

                if (result.Skipped)
                    _logger.LogDebug("Monthly questionnaire report {Year}-{Month} skipped: {Reason}", result.Year, result.Month, result.SkipReason);
                else
                    _logger.LogInformation("Monthly questionnaire report {Year}-{Month}: {Emails} emails queued across {Departments} departments.",
                        result.Year, result.Month, result.EmailsQueued, result.Departments.Count);
            }
            catch (Exception ex)
            {
                // Never let one bad run kill the loop — the next wake retries
                // (the month isn't claimed until the report is built).
                _logger.LogError(ex, "Monthly questionnaire report {Year}-{Month} failed.", target.Year, target.Month);
            }
        }
    }
}
