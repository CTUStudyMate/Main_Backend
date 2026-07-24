using System.ComponentModel.DataAnnotations;

namespace MainBackend.Models;

public class CreateReviewRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "courseId must be a positive integer.")]
    public int CourseId { get; set; }

    [Range(10, 50, ErrorMessage = "questionCount must be between 10 and 50.")]
    public int QuestionCount { get; set; }
}
