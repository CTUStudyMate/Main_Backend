namespace MainBackend.Models;

public class CuratedQa
{
    public int CuratedQaId { get; set; }
    public int VerifiableQaId { get; set; }
    public required string CuratedQuestion { get; set; }
    public required string CuratedAnswer { get; set; }
    public DateTime? CreatedAt { get; set; }

    public VerifiableQa VerifiableQa { get; set; } = null!;
}
