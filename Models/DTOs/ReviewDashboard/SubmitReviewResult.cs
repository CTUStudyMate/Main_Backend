namespace MainBackend.Models;

public class SubmitReviewResult
{
    public Guid ReviewSessionId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public DateTime CompletedAt { get; set; }

    public List<SubmittedQuestionResult> QuestionResults { get; set; } = [];
}

public class SubmittedQuestionResult
{
    public Guid QuestionResponseId { get; set; }
    public bool IsCorrect { get; set; }
}
