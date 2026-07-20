namespace MainBackend.Models.BackgroundWorker;

public enum BackgroundJobLogLevel
{
    Information,
    Warning,
    Error
}

public enum BackgroundJobLogEventType
{
    JobCreated,
    Started,
    RetryScheduled,
    Completed,
    Failed
}

public class BackgroundJobLog
{
    public Guid BackgroundJobLogId { get; set; }

    public Guid JobId { get; set; }
    public BackgroundJob Job { get; set; } = null!;

    public BackgroundJobLogLevel Level { get; set; }
    public BackgroundJobLogEventType EventType { get; set; }

    public int AttemptNumber { get; set; }
    public string Message { get; set; } = null!;
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }
}
