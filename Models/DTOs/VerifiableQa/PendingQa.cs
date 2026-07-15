namespace MainBackend.Models;

public class PendingQa
{
    public int VerifiableQaId { get; set; }
    public Guid MessageId { get; set; }
    public int? UserId { get; set; }
    public required string OriginalQuestion { get; set; }
    public string? RewrittenQuestion { get; set; }
    public required string GeneratedAnswer { get; set; }
    public VerifiableQaStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public PendingQaUser? User { get; set; }
    public ICollection<PendingQaCourse> Courses { get; set; } = new List<PendingQaCourse>();
}

public class PendingQaUser
{
    public int UserId { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public string? Cohort { get; set; }
    public required string AccountStatus { get; set; }
}

public class PendingQaCourse
{
    public int CourseId { get; set; }
    public required string CourseCode { get; set; }
    public required string CourseName { get; set; }
}
