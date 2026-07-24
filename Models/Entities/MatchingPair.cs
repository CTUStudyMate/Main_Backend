using Pgvector;

namespace MainBackend.Models;

public enum MatchingRelationType
{
    TermDefinition,
    ComponentFunction,
    EntityProperty,
    MethodPurpose,
    CauseEffect,
    ProcessResult,
    CategoryMember
}

public class MatchingPair
{
    public Guid MatchingPairId { get; set; }

    public int CuratedQaId { get; set; }
    public required string LeftText { get; set; }
    public required string RightText { get; set; }
    public MatchingRelationType RelationType { get; set; }
    public Vector? Embedding { get; set; }

    public DateTime CreatedAt { get; set; }

    public CuratedQa CuratedQa { get; set; } = null!;
}
