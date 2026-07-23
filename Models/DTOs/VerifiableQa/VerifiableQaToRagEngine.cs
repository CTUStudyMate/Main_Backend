using System.Text.Json.Serialization;
using MainBackend.Models.DTOs.VerifiableQa;

namespace MainBackend.Models;

public class VerifiableQaToRagEngine
{
    [JsonPropertyName("verifiable_qa_id")]
    public int VerifiableQaId { get; set; }

    [JsonPropertyName("original_question")]
    public required string OriginalQuestion { get; set; }

    [JsonPropertyName("rewritten_question")]
    public string? RewrittenQuestion { get; set; }

    [JsonPropertyName("generated_answer")]
    public required string GeneratedAnswer { get; set; }

    [JsonPropertyName("approved_answer")]
    public VerifiedAnswerDto? ApprovedAnswer { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("courses")]
    public ICollection<VerifiableQaCourseToRagEngine> Courses { get; set; } = [];
}

public class VerifiableQaCourseToRagEngine
{
    [JsonPropertyName("course_id")]
    public int CourseId { get; set; }

    [JsonPropertyName("course_code")]
    public required string CourseCode { get; set; }

    [JsonPropertyName("course_name")]
    public required string CourseName { get; set; }
}
