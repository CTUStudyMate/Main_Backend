namespace MainBackend.Models;

public class PauseReviewRequest
{
    public Guid ReviewSessionId { get; set; }
    public List<PauseQuestionResponseRequest> QuestionResponses { get; set; } = [];
}

public class PauseQuestionResponseRequest
{
    public Guid QuestionResponseId { get; set; }
    public required string StudentAnswer { get; set; }
}
