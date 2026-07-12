namespace MainBackend.Models;

public class ReviewSession
{
    public Guid ReviewSessionId { get; set; }
    public int UserId { get; set; }
    public int? CourseId { get; set; }
    public int TotalQuestions { get; set; }
    public int? CorrectAnswers { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }

    public User User { get; set; } = null!;
    public Course? Course { get; set; }
    public ICollection<QuestionResponse> QuestionResponses { get; set; } = new List<QuestionResponse>();
}
