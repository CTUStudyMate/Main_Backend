using System.ComponentModel.DataAnnotations;

namespace MainBackend.Models;

public class CreateReviewRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "courseId must be a positive integer.")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "questionCount must be a positive integer.")]
    public int QuestionCount { get; set; }

    public bool IsRandom { get; set; } = true;

    [Range(0, int.MaxValue, ErrorMessage = "mcqQuestionCount cannot be negative.")]
    public int McqQuestionCount { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "fillBlankQuestionCount cannot be negative.")]
    public int FillBlankQuestionCount { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "matchingQuestionCount cannot be negative.")]
    public int MatchingQuestionCount { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsRandom)
        {
            yield break;
        }

        var requestedQuestionCount =
            McqQuestionCount + FillBlankQuestionCount + MatchingQuestionCount;

        if (requestedQuestionCount < 1)
        {
            yield return new ValidationResult(
                "Select at least one question.",
                [
                    nameof(McqQuestionCount),
                    nameof(FillBlankQuestionCount),
                    nameof(MatchingQuestionCount)
                ]);
        }
    }
}
