namespace MainBackend.Models;

public enum QuestionItemType
{
    Mcq,
    Matching,
    FillBlank
}

public class QuestionItem
{
    public Guid QuestionItemId { get; set; }
    public int CuratedQaId { get; set; }
    public QuestionItemType Type { get; set; }
    public required string QuestionData { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? CreatedAt { get; set; }

    public CuratedQa CuratedQa { get; set; } = null!;
    public ICollection<QuestionResponse> QuestionResponses { get; set; } = new List<QuestionResponse>();
}
