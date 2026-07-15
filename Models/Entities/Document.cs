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

public class Document
{
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public required string DocumentTitle { get; set; }
    public required string FileUrl { get; set; }
    public DocumentSourceType SourceType { get; set; }
    public DocumentVisibility Visibility { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
