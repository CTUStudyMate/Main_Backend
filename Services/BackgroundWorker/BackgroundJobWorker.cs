using MainBackend.Models.BackgroundWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services.BackgroundWorker;

public class BackgroundJobWorker : BackgroundService
{
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
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var pendingJobs = await db.BackgroundJobs
                .Where(job =>
                    job.Status == BackgroundJobStatus.Pending &&
                    (job.NextAttemptAt == null || job.NextAttemptAt <= now))
                .OrderBy(job => job.CreatedAt)
                .Take(10)
                .ToListAsync(stoppingToken);
        }
    }
}
