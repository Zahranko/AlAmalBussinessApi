using AlAmalBusiness.Application.DTOs.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AlAmalBusiness.Api.Email
{
    // Drains ChannelEmailQueue and sends each message over SMTP. Runs inside
    // the web app's own process (an IHostedService, not a separate service),
    // which is what the shared host allows.
    public sealed class SmtpEmailBackgroundService : BackgroundService
    {
        // Delays before the 2nd and 3rd attempt; a message still failing
        // after that is logged and dropped.
        private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1) };

        private readonly ChannelEmailQueue _queue;
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailBackgroundService> _logger;

        public SmtpEmailBackgroundService(
            ChannelEmailQueue queue,
            IOptions<EmailSettings> settings,
            ILogger<SmtpEmailBackgroundService> logger)
        {
            _queue = queue;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.IsUsable)
            {
                _logger.LogWarning("Email is disabled or not fully configured (Email section); no emails will be sent.");
                return;
            }

            try
            {
                await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
                    await SendWithRetryAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App shutting down.
            }
        }

        private async Task SendWithRetryAsync(EmailMessage message, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await SendAsync(message, ct);
                    _logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex) when (attempt < RetryDelays.Length && !IsPermanent(ex))
                {
                    _logger.LogWarning(ex, "Email to {To} failed (attempt {Attempt}), retrying.", message.To, attempt + 1);
                    await Task.Delay(RetryDelays[attempt], ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email to {To} failed permanently: {Subject}", message.To, message.Subject);
                    return;
                }
            }
        }

        // Retrying can't fix a wrong password or a rejected address.
        private static bool IsPermanent(Exception ex) =>
            ex is AuthenticationException
            || ex is SmtpCommandException { StatusCode: >= (SmtpStatusCode)500 };

        private async Task SendAsync(EmailMessage message, CancellationToken ct)
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            mime.To.Add(MailboxAddress.Parse(message.To));
            mime.Subject = message.Subject;
            mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

            // 465 is implicit TLS; anything else (587) upgrades with STARTTLS.
            // Never falls back to plaintext.
            var security = _settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            using var client = new SmtpClient { Timeout = 30_000 };
            await client.ConnectAsync(_settings.Host, _settings.Port, security, ct);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
