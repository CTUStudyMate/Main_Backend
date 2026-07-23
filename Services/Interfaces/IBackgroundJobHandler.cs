using MainBackend.Models.BackgroundWorker;

namespace MainBackend.Services.BackgroundWorker;

public interface IBackgroundJobHandler
{
    BackgroundJobType JobType { get; }

    Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken, AppDbContext db);
}