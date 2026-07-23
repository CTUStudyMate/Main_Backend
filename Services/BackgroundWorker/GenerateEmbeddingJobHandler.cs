using MainBackend.Models;
using MainBackend.Models.BackgroundWorker;
using MainBackend.Services;
using MainBackend.Services.BackgroundWorker;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

public class GenerateEmbeddingJobHandler : IBackgroundJobHandler
{
    private const double DuplicateSimilarityThreshold = 0.85;

    private sealed record DuplicateMatch(
        VerifiableQa VerifiableQa,
        double Similarity);

    public BackgroundJobType JobType => BackgroundJobType.GenerateEmbedding;

    private readonly IEmbeddingService embeddingService;

    public GenerateEmbeddingJobHandler(IEmbeddingService embedding)
    {
        embeddingService = embedding;
    }

    public async Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken, AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var verifiableQa = await db.VerifiableQas
            .FirstOrDefaultAsync(
                v => v.VerifiableQaId == job.VerifiableQaId,
                cancellationToken
            );

        if (verifiableQa is null)
        {
            db.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Error,
                EventType = BackgroundJobLogEventType.Failed,
                AttemptNumber = job.AttemptCount,
                Message = $"FAILED: Background job {job.JobId}: The job is generating embedding for a verifiable qa but no qa provided.",
                CreatedAt = now
            });
            job.UpdatedAt = now;
            job.Status = BackgroundJobStatus.Failed;
            job.CompletedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(verifiableQa.RewrittenQuestion))
        {
            if (string.IsNullOrWhiteSpace(verifiableQa.OriginalQuestion))
            {
                db.BackgroundJobLogs.Add(new BackgroundJobLog
                {
                    BackgroundJobLogId = Guid.NewGuid(),
                    JobId = job.JobId,
                    Level = BackgroundJobLogLevel.Error,
                    EventType = BackgroundJobLogEventType.Failed,
                    AttemptNumber = job.AttemptCount,
                    Message = $"FAILED: Background job {job.JobId}: The job is generating embedding for a verifiable qa but qa has no question.",
                    CreatedAt = now
                });
                job.UpdatedAt = now;
                job.Status = BackgroundJobStatus.Failed;
                job.CompletedAt = now;
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

        }


        float[] embeddedContent = await embeddingService.GenerateEmbeddingAsync((verifiableQa.RewrittenQuestion is not null && !string.IsNullOrWhiteSpace(verifiableQa.RewrittenQuestion)) ? verifiableQa.RewrittenQuestion : verifiableQa.OriginalQuestion, cancellationToken: cancellationToken);
        
        var matchQA = await FindDuplicateVerifiableQaAsync(embeddedContent, verifiableQa.VerifiableQaId, db, cancellationToken);

        // already has similar question in database
        if (matchQA is not null)
        {
            job.Status = BackgroundJobStatus.Completed;
            job.UpdatedAt = now;
            job.NextAttemptAt = null;
            job.CompletedAt = now;

            db.BackgroundJobLogs.Add(new BackgroundJobLog
            {
                BackgroundJobLogId = Guid.NewGuid(),
                JobId = job.JobId,
                Level = BackgroundJobLogLevel.Information,
                EventType = BackgroundJobLogEventType.SkippedDuplicate,
                AttemptNumber = job.AttemptCount,
                Message = $"Background job {job.JobId} skipped because it matches VerifiableQa {matchQA.VerifiableQa.VerifiableQaId}.",
                Details = $"Cosine similarity: {matchQA.Similarity:F6}; threshold: {DuplicateSimilarityThreshold:F2}.",
                CreatedAt = now
            });

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // main flow: generate embedding, log, update current job to complete, 
        //              and create curatedqa job

        verifiableQa.Embedding = new Vector(embeddedContent);

        job.Status = BackgroundJobStatus.Completed;
        job.UpdatedAt = now;
        job.NextAttemptAt = null;
        job.CompletedAt = now;

        db.BackgroundJobLogs.Add(new BackgroundJobLog
        {
            BackgroundJobLogId = Guid.NewGuid(),
            JobId = job.JobId,
            Level = BackgroundJobLogLevel.Information,
            EventType = BackgroundJobLogEventType.Completed,
            AttemptNumber = job.AttemptCount,
            Message = $"Background job {job.JobId} completed: {job.Type}",
            CreatedAt = DateTime.UtcNow
        });

        var curatedQaJob = new BackgroundJob
        {
            AttemptCount = 0,
            CreatedAt = now,
            JobId = Guid.NewGuid(),
            MaxAttempts = 5,
            SourceEntityType = BackgroundJobSourceEntityType.VerifiableQa,
            VerifiableQaId = job.VerifiableQaId,
            Status = BackgroundJobStatus.Pending,
            Type = BackgroundJobType.GenerateCuratedQa,
            UpdatedAt = now,
        };
        db.BackgroundJobs.Add(curatedQaJob);

        db.BackgroundJobLogs.Add(new BackgroundJobLog
        {
            BackgroundJobLogId = Guid.NewGuid(),
            JobId = curatedQaJob.JobId,
            Level = BackgroundJobLogLevel.Information,
            EventType = BackgroundJobLogEventType.JobCreated,
            AttemptNumber = 0,
            Message = $"Background job created: {BackgroundJobType.GenerateCuratedQa} for VerifiableQa {job.VerifiableQaId}.",
            CreatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);

    }

    private async Task<DuplicateMatch?> FindDuplicateVerifiableQaAsync(
        float[] embedding,
        int currentVerifiableQaId,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var newEmbedding = new Vector(embedding);

        var closestQa = await db.VerifiableQas
            .AsNoTracking()
            .Where(qa =>
                qa.VerifiableQaId != currentVerifiableQaId &&
                qa.Status == VerifiableQaStatus.Approved &&
                qa.Embedding != null)
            .OrderBy(qa => qa.Embedding!.CosineDistance(newEmbedding))
            .Select(qa => new
            {
                VerifiableQa = qa,
                CosineDistance = qa.Embedding!.CosineDistance(newEmbedding)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (closestQa is null)
        {
            return null;
        }

        var similarity = 1 - closestQa.CosineDistance;
        return similarity >= DuplicateSimilarityThreshold
            ? new DuplicateMatch(closestQa.VerifiableQa, similarity)
            : null;
    }
}
