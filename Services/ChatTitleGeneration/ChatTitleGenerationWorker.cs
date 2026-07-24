using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services.ChatTitleGeneration;

public sealed class ChatTitleGenerationWorker : BackgroundService
{
    private readonly IChatTitleJobQueue _jobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatTitleGenerationWorker> _logger;

    public ChatTitleGenerationWorker(
        IChatTitleJobQueue jobQueue,
        IServiceScopeFactory scopeFactory,
        ILogger<ChatTitleGenerationWorker> logger)
    {
        _jobQueue = jobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _jobQueue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to generate a title for chat {ChatId}.",
                    job.ChatId);
            }
        }
    }

    private async Task ProcessJobAsync(
        ChatTitleJob job,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var titleGenerator = scope.ServiceProvider
            .GetRequiredService<IChatTitleGenerator>();

        var title = await titleGenerator.GenerateTitleAsync(
            job.TitleSource,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException(
                "The chat title generator returned an empty title.");
        }

        // The ownership and marker checks prevent a stale or duplicate job from
        // overwriting a title that has already been updated.
        await db.Chats
            .Where(chat =>
                chat.ChatId == job.ChatId &&
                chat.UserId == job.UserId &&
                chat.ChatTitle == ChatService.GeneratingTitleMarker)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    chat => chat.ChatTitle,
                    title),
                cancellationToken);
    }
}
