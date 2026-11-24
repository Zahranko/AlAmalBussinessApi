using AlAmalBusiness.Domain.IRepositories;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace AlAmalBusiness.Api.Push
{
    // Drains ChannelPushQueue: resolves each job to device tokens (one query,
    // the email recipient rules) and sends through Firebase Cloud Messaging,
    // which delivers to Android directly and to iPhones through APNs. Tokens
    // FCM reports as dead (app uninstalled, token rotated) are deleted, so
    // the table only holds phones that can still be reached.
    public sealed class FcmPushBackgroundService : BackgroundService
    {
        // FCM's per-call ceiling for a multicast.
        private const int BatchSize = 500;

        private readonly ChannelPushQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly PushSettings _settings;
        private readonly ILogger<FcmPushBackgroundService> _logger;

        public FcmPushBackgroundService(
            ChannelPushQueue queue,
            IServiceScopeFactory scopes,
            IOptions<PushSettings> settings,
            ILogger<FcmPushBackgroundService> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_settings.IsUsable)
            {
                _logger.LogWarning("Push is disabled or not configured (Push:ServiceAccountJson); no phone notifications will be sent.");
                return;
            }

            FirebaseMessaging messaging;
            try
            {
                var credential = CredentialFactory
                    .FromJson<ServiceAccountCredential>(_settings.ServiceAccountJson!)
                    .ToGoogleCredential()
                    .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                // A named app, so nothing else in the process can collide with it.
                var app = FirebaseApp.GetInstance("alamal-push")
                    ?? FirebaseApp.Create(new AppOptions { Credential = credential }, "alamal-push");
                messaging = FirebaseMessaging.GetMessaging(app);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Push: the Firebase service account could not be loaded; push is off.");
                return;
            }

            try
            {
                await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await SendAsync(messaging, job, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Push {Kind} {Id} failed.", job.Message.Kind, job.Message.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutting down
            }
        }

        private async Task SendAsync(FirebaseMessaging messaging, PushJob job, CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var tokens = scope.ServiceProvider.GetRequiredService<IDeviceTokenRepo>();

            List<string> targets;
            if (job.UserIds != null) targets = await tokens.GetTokensForUsersAsync(job.UserIds);
            else if (job.Role != null && job.DepartmentId.HasValue) targets = await tokens.GetTokensInRoleAsync(job.Role, job.DepartmentId.Value);
            else if (job.Roles != null) targets = await tokens.GetTokensInRolesAsync(job.Roles, job.ExcludeUserId);
            else return;

            if (targets.Count == 0)
            {
                _logger.LogInformation("Push {Kind} {Id}: no registered phone to tell.", job.Message.Kind, job.Message.Id);
                return;
            }

            var m = job.Message;
            var data = new Dictionary<string, string>(m.Extra) { ["kind"] = m.Kind, ["id"] = m.Id };
            var dead = new List<string>();
            var sent = 0;

            foreach (var batch in targets.Chunk(BatchSize))
            {
                var message = new MulticastMessage
                {
                    Tokens = batch,
                    Notification = new Notification { Title = m.Title, Body = m.Body },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        // High priority wakes a dozing phone — these are things
                        // somebody is waiting on, not a newsletter.
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            // Must match the channel the app creates (PushService).
                            ChannelId = "alamal_alerts",
                            Tag = m.Thread,
                            Sound = "default",
                        },
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string> { ["apns-priority"] = "10" },
                        Aps = new Aps
                        {
                            Sound = "default",
                            ThreadId = m.Thread,
                            // Lets the app's notification service update the badge.
                            MutableContent = true,
                        },
                    },
                };

                var result = await messaging.SendEachForMulticastAsync(message, ct);
                sent += result.SuccessCount;
                for (var i = 0; i < result.Responses.Count; i++)
                {
                    var r = result.Responses[i];
                    if (r.IsSuccess) continue;
                    var code = r.Exception?.MessagingErrorCode;
                    if (code is MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch
                        || (code == MessagingErrorCode.InvalidArgument && r.Exception?.Message.Contains("registration token", StringComparison.OrdinalIgnoreCase) == true))
                        dead.Add(batch[i]);
                    else
                        _logger.LogWarning("Push {Kind} {Id}: one device refused ({Code}).", m.Kind, m.Id, code);
                }
            }

            if (dead.Count > 0) await tokens.RemoveTokensAsync(dead);
            _logger.LogInformation("Push {Kind} {Id}: sent to {Sent} of {Total} phones, {Dead} dead tokens removed.",
                m.Kind, m.Id, sent, targets.Count, dead.Count);
        }
    }
}
