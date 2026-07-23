using MainBackend.Models.BackgroundWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services.BackgroundWorker;

public class BackgroundJobWorker : BackgroundService
{
    private static readonly TimeSpan[] ErrorRetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6)
    ];

    private readonly IServiceScopeFactory _scopeFactory;

    public BackgroundJobWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = DateTime.UtcNow;
            List<Guid> candidateJobIds;
            using (var discoveryScope = _scopeFactory.CreateScope())
            {
                var discoveryDb = discoveryScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                candidateJobIds = await discoveryDb.BackgroundJobs
                    .Where(job =>
                        (job.Status == BackgroundJobStatus.Pending || job.Status == BackgroundJobStatus.RetryPending) &&
                        (job.NextAttemptAt == null || job.NextAttemptAt <= now))
                    .OrderBy(job => job.CreatedAt)
                    .Take(10)
                    .Select(job => job.JobId)
                    .ToListAsync(stoppingToken);
            }

            foreach (var jobId in candidateJobIds)
            {
                using var jobScope = _scopeFactory.CreateScope();
                var jobDb = jobScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var handlers = jobScope.ServiceProvider.GetServices<IBackgroundJobHandler>();
                var job = await jobDb.BackgroundJobs
                    .FirstOrDefaultAsync(
                        job =>
                            job.JobId == jobId &&
                            (job.Status == BackgroundJobStatus.Pending ||
                             job.Status == BackgroundJobStatus.RetryPending) &&
                            (job.NextAttemptAt == null || job.NextAttemptAt <= now),
                        stoppingToken);

                if (job is null)
                {
                    continue;
                }

                job.Status = BackgroundJobStatus.Processing;
                job.AttemptCount++;
                job.UpdatedAt = now;

                jobDb.BackgroundJobLogs.Add(new BackgroundJobLog
                {
                    BackgroundJobLogId = Guid.NewGuid(),
                    JobId = job.JobId,
                    Level = BackgroundJobLogLevel.Information,
                    EventType = BackgroundJobLogEventType.Started,
                    AttemptNumber = job.AttemptCount,
                    Message = $"Background job {job.JobId} started: " +
                        $"{job.Type} for {job.SourceEntityType} " +
                        $"{(job.VerifiableQaId.HasValue
                            ? job.VerifiableQaId.Value
                            : job.CuratedQaId)}",
                    CreatedAt = now
                });
                await jobDb.SaveChangesAsync(stoppingToken);

                var handler = handlers.SingleOrDefault(
                    handler => handler.JobType == job.Type);

                if (handler is null)
                {
                    await HandleJobFailureAsync(
                        job.JobId,
                        new InvalidOperationException(
                            $"No handler is registered for background job type {job.Type}."),
                        stoppingToken);
                    continue;
                }

                try
                {
                    await handler.HandleAsync(job, stoppingToken, jobDb);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    await HandleJobFailureAsync(job.JobId, ex, stoppingToken);
                }
            }
        }
    }

    private async Task HandleJobFailureAsync(
        Guid jobId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        using var retryScope = _scopeFactory.CreateScope();
        var retryDb = retryScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await retryDb.BackgroundJobs
            .FirstOrDefaultAsync(job => job.JobId == jobId, cancellationToken);

        if (job is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (job.AttemptCount < job.MaxAttempts)
        {
            var delayIndex = Math.Min(job.AttemptCount - 1, ErrorRetryDelays.Length - 1);
            job.Status = BackgroundJobStatus.RetryPending;
            job.UpdatedAt = now;
            job.NextAttemptAt = now + ErrorRetryDelays[delayIndex];

            retryDb.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Error,
                EventType = BackgroundJobLogEventType.RetryScheduled,
                AttemptNumber = job.AttemptCount,
                Message = $"Background job {job.JobId} failed. Next retry at {job.NextAttemptAt}.",
                Details = exception.ToString(),
                CreatedAt = now
            });
        }
        else
        {
            job.Status = BackgroundJobStatus.Failed;
            job.UpdatedAt = now;
            job.NextAttemptAt = null;
            job.CompletedAt = now;

            retryDb.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Error,
                EventType = BackgroundJobLogEventType.Failed,
                AttemptNumber = job.AttemptCount,
                Message = $"Background job {job.JobId} failed after all attempts.",
                Details = exception.ToString(),
                CreatedAt = now
            });
        }

        await retryDb.SaveChangesAsync(cancellationToken);
    }
}
