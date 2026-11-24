using AlAmalBusiness.Application.DTOs.Push;
using AlAmalBusiness.Application.Services.Interface;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace AlAmalBusiness.Api.Push
{
    // Who a push is for, resolved to device tokens later on the sender.
    public sealed record PushJob(
        PushMessage Message,
        IReadOnlyCollection<string>? UserIds = null,
        string? Role = null,
        int? DepartmentId = null,
        IReadOnlyCollection<string>? Roles = null,
        string? ExcludeUserId = null);

    // The ChannelEmailQueue arrangement: an in-memory, bounded hand-off from
    // the request to FcmPushBackgroundService. Queued pushes still waiting
    // when the container restarts are lost — acceptable, the record itself is
    // saved and in its inbox, and the email still goes.
    public sealed class ChannelPushQueue : IPushNotifier
    {
        private const int Capacity = 1000;

        private readonly Channel<PushJob> _channel = Channel.CreateBounded<PushJob>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true
            });

        private readonly ILogger<ChannelPushQueue> _logger;

        public ChannelPushQueue(IOptions<PushSettings> settings, ILogger<ChannelPushQueue> logger)
        {
            IsEnabled = settings.Value.IsUsable;
            _logger = logger;
        }

        public bool IsEnabled { get; }

        public ChannelReader<PushJob> Reader => _channel.Reader;

        public void ToUsers(IReadOnlyCollection<string> userIds, PushMessage message)
        {
            if (userIds.Count > 0) Write(new PushJob(message, UserIds: userIds.ToArray()));
        }

        public void ToRoleInDepartment(string role, int departmentId, PushMessage message) =>
            Write(new PushJob(message, Role: role, DepartmentId: departmentId));

        public void ToRoles(IReadOnlyCollection<string> roles, string? excludeUserId, PushMessage message)
        {
            if (roles.Count > 0) Write(new PushJob(message, Roles: roles.ToArray(), ExcludeUserId: excludeUserId));
        }

        private void Write(PushJob job)
        {
            if (!IsEnabled) return;
            if (!_channel.Writer.TryWrite(job))
                _logger.LogWarning("Push queue full; dropped {Kind} {Id}.", job.Message.Kind, job.Message.Id);
        }
    }
}
