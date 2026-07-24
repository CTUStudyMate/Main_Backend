namespace MainBackend.Services.ChatTitleGeneration;

public interface IChatTitleJobQueue
{
    ValueTask EnqueueAsync(
        ChatTitleJob job,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChatTitleJob> ReadAllAsync(
        CancellationToken cancellationToken = default);
}
