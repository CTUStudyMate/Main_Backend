using Pgvector;

namespace MainBackend.Models;

public enum VerifiableQaStatus
{
    Pending,
    Approved
}

public class VerifiableQa
{
    public int VerifiableQaId { get; set; }
    public Guid SourceMessageId { get; set; }
    public int? UserId { get; set; }
    public required string OriginalQuestion { get; set; }
    public string? RewrittenQuestion { get; set; }
    public required string GeneratedAnswer { get; set; }
    public VerifiableQaStatus Status { get; set; }
    public string? ApprovedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Vector? Embedding { get; set; }

    public Message SourceMessage { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<CuratedQa> CuratedQas { get; set; } = new List<CuratedQa>();
}
