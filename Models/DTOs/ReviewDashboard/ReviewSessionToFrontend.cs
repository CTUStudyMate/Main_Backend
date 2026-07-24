namespace MainBackend.Models;

public class ReviewSessionToFrontend
{
    public Guid ReviewSessionId { get; set; }
    public int? CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseName { get; set; }
    public int TotalQuestions { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool IsCompleted { get; set; }

    public List<QuestionResponseToFrontend> QuestionResponses { get; set; } = [];
}
