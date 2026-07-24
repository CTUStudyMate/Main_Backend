using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class CuratedQaToRagEngine
{
    [JsonPropertyName("curated_qa_id")]
    public int CuratedQaId { get; set; }

    [JsonPropertyName("question")]
    public required string Question { get; set; }

    [JsonPropertyName("answer")]
    public required string Answer { get; set; }

    [JsonPropertyName("course_names")]
    public List<string> CourseNames { get; set; } = [];
}
