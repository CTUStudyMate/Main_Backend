namespace MainBackend.Models;

public enum DocumentSourceType
{
    System,
    User
}

public enum DocumentVisibility
{
    Active,
    Suspended
}

public enum DocumentProcessingStatus
{
    Queued,
    Parsing,
    Chunking,
    Indexing,
    Ready,
    Failed
}

public class Document
{
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public required string DocumentTitle { get; set; }
    public required string FileUrl { get; set; }
    public string? StorageKey { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DocumentSourceType SourceType { get; set; }
    public DocumentVisibility Visibility { get; set; }
    public DocumentProcessingStatus ProcessingStatus { get; set; } = DocumentProcessingStatus.Ready;
    public Guid? ProcessingRunId { get; set; }
    public int ProcessingProgress { get; set; }
    public string? ProcessingMessage { get; set; }
    public DateTime? ProcessingUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
