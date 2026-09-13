using AlAmalBusiness.Application.DTOs.Email;
using AlAmalBusiness.Application.Services.Interface;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace AlAmalBusiness.Api.Email
{
    // In-process queue between the request that wants an email sent and
    // SmtpEmailBackgroundService, which does the sending. No external queue
    // service exists on the shared host, so this is memory-only: messages
    // still waiting when the app pool recycles are lost. That is acceptable
    // for a notification — the feedback itself is already in the database
    // and visible in the inbox.
    public sealed class ChannelEmailQueue : IEmailQueue
    {
        // Bounded so an SMTP outage during a burst can't grow memory without
        // limit on the 1 GB pool; past this, new messages are dropped.
        private const int Capacity = 500;

        private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true
            });

        private readonly bool _enabled;

        public ChannelEmailQueue(IOptions<EmailSettings> settings)
        {
            _enabled = settings.Value.IsUsable;
        }

        public ChannelReader<EmailMessage> Reader => _channel.Reader;

        public bool IsEnabled => _enabled;

        public bool Enqueue(EmailMessage message) =>
            _enabled && _channel.Writer.TryWrite(message);
    }
}
