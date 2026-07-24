using System.Threading.Channels;

namespace MainBackend.Services.ChatTitleGeneration;

public sealed class ChatTitleJobQueue : IChatTitleJobQueue
{
    private readonly Channel<ChatTitleJob> _jobs =
        Channel.CreateUnbounded<ChatTitleJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

    public ValueTask EnqueueAsync(
        ChatTitleJob job,
        CancellationToken cancellationToken = default)
    {
        return _jobs.Writer.WriteAsync(job, cancellationToken);
    }

    public IAsyncEnumerable<ChatTitleJob> ReadAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _jobs.Reader.ReadAllAsync(cancellationToken);
    }
}
