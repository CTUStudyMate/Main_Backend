using System.Text.Json.Serialization;
namespace MainBackend.Models;

public class RagResponse
{
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("segments")]
    public required List<RagSegment> Segments { get; set; }
}