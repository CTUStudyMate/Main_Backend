using System.Text.Json;

namespace MainBackend.Models;

public class SubmitReviewRequest
{
    public Guid ReviewSessionId { get; set; }
    public List<SubmitQuestionResponseRequest> QuestionResponses { get; set; } = [];
}

public class SubmitQuestionResponseRequest
{
    public Guid QuestionResponseId { get; set; }
    public JsonElement StudentAnswer { get; set; }
}
