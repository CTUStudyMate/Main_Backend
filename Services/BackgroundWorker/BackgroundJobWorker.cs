using MainBackend.Models.BackgroundWorker;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services.BackgroundWorker;

public class BackgroundJobWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval =
        TimeSpan.FromSeconds(5);
    private static readonly TimeSpan PollingErrorDelay =
        TimeSpan.FromSeconds(10);
    private static readonly TimeSpan[] ErrorRetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromHours(1),
        TimeSpan.FromHours(6)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobWorker> _logger;

    public BackgroundJobWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundJobWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await ProcessAvailableJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Background job polling failed. The worker will retry in {DelaySeconds} seconds.",
                    PollingErrorDelay.TotalSeconds);

                try
                {
                    await Task.Delay(PollingErrorDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task ProcessAvailableJobsAsync(
        CancellationToken stoppingToken)
    {
        var now = DateTime.UtcNow;
        List<Guid> candidateJobIds;

        using (var discoveryScope = _scopeFactory.CreateScope())
        {
            var discoveryDb = discoveryScope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            candidateJobIds = await discoveryDb.BackgroundJobs
                .Where(job =>
                    (job.Status == BackgroundJobStatus.Pending ||
                     job.Status == BackgroundJobStatus.RetryPending) &&
                    (job.NextAttemptAt == null ||
                     job.NextAttemptAt <= now))
                .OrderBy(job => job.CreatedAt)
                .Take(10)
                .Select(job => job.JobId)
                .ToListAsync(stoppingToken);
        }

        foreach (var jobId in candidateJobIds)
        {
            await ProcessJobAsync(jobId, now, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(
        Guid jobId,
        DateTime now,
        CancellationToken stoppingToken)
    {
        using var jobScope = _scopeFactory.CreateScope();
        var jobDb = jobScope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        var handlers = jobScope.ServiceProvider
            .GetServices<IBackgroundJobHandler>();
        var job = await jobDb.BackgroundJobs
            .FirstOrDefaultAsync(
                item =>
                    item.JobId == jobId &&
                    (item.Status == BackgroundJobStatus.Pending ||
                     item.Status == BackgroundJobStatus.RetryPending) &&
                    (item.NextAttemptAt == null ||
                     item.NextAttemptAt <= now),
                stoppingToken);

        if (job is null)
        {
            return;
        }

        var timeline = new BackgroundJobTimestampSequence();
        job.Status = BackgroundJobStatus.Processing;
        job.AttemptCount++;
        job.UpdatedAt = timeline.Next();

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
            CreatedAt = timeline.Next()
        });
        await jobDb.SaveChangesAsync(stoppingToken);

        var handler = handlers.SingleOrDefault(
            item => item.JobType == job.Type);

        if (handler is null)
        {
            await HandleJobFailureAsync(
                job.JobId,
                new InvalidOperationException(
                    $"No handler is registered for background job type {job.Type}."),
                stoppingToken);
            return;
        }

        try
        {
            await handler.HandleAsync(job, stoppingToken, jobDb);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await HandleJobFailureAsync(
                job.JobId,
                exception,
                stoppingToken);
        }
    }

    private async Task HandleJobFailureAsync(
        Guid jobId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        using var retryScope = _scopeFactory.CreateScope();
        var retryDb = retryScope.ServiceProvider
            .GetRequiredService<AppDbContext>();
        var job = await retryDb.BackgroundJobs
            .FirstOrDefaultAsync(
                item => item.JobId == jobId,
                cancellationToken);

        if (job is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var timeline = new BackgroundJobTimestampSequence();
        if (job.AttemptCount < job.MaxAttempts)
        {
            var delayIndex = Math.Min(
                job.AttemptCount - 1,
                ErrorRetryDelays.Length - 1);
            job.Status = BackgroundJobStatus.RetryPending;
            job.UpdatedAt = timeline.Next();
            job.NextAttemptAt = now + ErrorRetryDelays[delayIndex];

            retryDb.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Error,
                EventType = BackgroundJobLogEventType.RetryScheduled,
                AttemptNumber = job.AttemptCount,
                Message = $"Background job {job.JobId} failed. " +
                    $"Next retry at {job.NextAttemptAt}.",
                Details = exception.ToString(),
                CreatedAt = timeline.Next()
            });
        }
        else
        {
            job.Status = BackgroundJobStatus.Failed;
            job.UpdatedAt = timeline.Next();
            job.NextAttemptAt = null;
            job.CompletedAt = timeline.Next();

            retryDb.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Error,
                EventType = BackgroundJobLogEventType.Failed,
                AttemptNumber = job.AttemptCount,
                Message = $"Background job {job.JobId} failed after all attempts.",
                Details = exception.ToString(),
                CreatedAt = timeline.Next()
            });
        }

        await retryDb.SaveChangesAsync(cancellationToken);
    }
}
