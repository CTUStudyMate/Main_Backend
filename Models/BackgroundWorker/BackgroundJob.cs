using MainBackend.Models;

namespace MainBackend.Models.BackgroundWorker;

public enum BackgroundJobType
{
    GenerateEmbedding,
    GenerateCuratedQa,
    GenerateExercises
}

public enum BackgroundJobStatus
{
    Pending,
    Processing,
    RetryPending,
    Completed,
    Failed,
}

public enum BackgroundJobSourceEntityType
{
    VerifiableQa,
    CuratedQa
}

public class BackgroundJob
{
    public Guid JobId { get; set; }

    public BackgroundJobType Type { get; set; }
    public BackgroundJobStatus Status { get; set; }

    public string? BusinessData { get; set; }

    public BackgroundJobSourceEntityType SourceEntityType { get; set; }
    public int? VerifiableQaId { get; set; }
    public VerifiableQa? VerifiableQa { get; set; }
    public int? CuratedQaId { get; set; }
    public CuratedQa? CuratedQa { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public DateTime? NextAttemptAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
