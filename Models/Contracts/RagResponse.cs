using System.Text.Json.Serialization;
namespace MainBackend.Models;

public class RagResponse
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("segments")]
    public required List<RagSegment> Segments { get; set; }

    [JsonPropertyName("need_verify")]
    public required bool NeedVerify { get; set; }

    [JsonPropertyName("rewritten_question")]
    public string? RewrittenQuestion { get; set; }

    [JsonPropertyName("document_ids")]
    public List<string> DocumentIds { get; set; } = new();
}