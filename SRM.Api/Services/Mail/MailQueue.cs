using System.Threading.Channels;

namespace SRM.Api.Services.Mail
{
    public sealed class MailQueue
    {
        private readonly Channel<QueuedMail> _channel =
            Channel.CreateBounded<QueuedMail>(new BoundedChannelOptions(200)
            {
                FullMode = BoundedChannelFullMode.Wait, // TryWrite returns false when full
                SingleReader = true
            });

        public bool TryEnqueue(QueuedMail mail) => _channel.Writer.TryWrite(mail);

        public IAsyncEnumerable<QueuedMail> ReadAllAsync(CancellationToken ct) =>
            _channel.Reader.ReadAllAsync(ct);
    }
}
