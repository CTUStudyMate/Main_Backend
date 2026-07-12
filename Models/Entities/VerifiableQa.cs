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
    public Guid MessageId { get; set; }
    public int? UserId { get; set; }
    public required string OriginalQuestion { get; set; }
    public string? RewrittenQuestion { get; set; }
    public required string GeneratedAnswer { get; set; }
    public VerifiableQaStatus Status { get; set; }
    public string? ApprovedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Vector? Embedding { get; set; }

    public Message Message { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<CuratedQa> CuratedQas { get; set; } = new List<CuratedQa>();
}
