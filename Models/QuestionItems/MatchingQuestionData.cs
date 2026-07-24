namespace MainBackend.Models.QuestionItems;

public class MatchingItem
{
    public required string Id { get; set; }
    public required string Text { get; set; }
}

public class MatchingCorrectPair
{
    public required string LeftId { get; set; }
    public required string RightId { get; set; }
}

public class MatchingQuestionData
{
    public int Version { get; set; } = 1;
    public required string Instruction { get; set; }
    public required string Question { get; set; }
    public List<MatchingItem> LeftItems { get; set; } = [];
    public List<MatchingItem> RightItems { get; set; } = [];
    public List<MatchingCorrectPair> CorrectPairs { get; set; } = [];
    public List<Guid> SourceMatchingPairIds { get; set; } = [];
}
