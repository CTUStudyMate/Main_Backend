namespace MainBackend.Models;

public class QuestionResponse
{
    public Guid QuestionResponseId { get; set; }
    public Guid QuestionItemId { get; set; }
    public Guid ReviewSessionId { get; set; }
    public required string StudentAnswer { get; set; }
    public bool? IsCorrect { get; set; }

    public QuestionItem QuestionItem { get; set; } = null!;
    public ReviewSession ReviewSession { get; set; } = null!;
}
