using System.Text.Json;
namespace MainBackend.Models;

using System.Text.Json.Serialization;

public class RagSegment
{
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }


    [JsonPropertyName("segment")]
    public required string Segment { get; set; }


    [JsonPropertyName("citations")]
    public List<RagCitation>? Citations { get; set; }

    
    [JsonPropertyName("processed_cite_obj")]
    public ProcessedCiteObj? ProcessedCiteObj { get; set; }

    // fallback when schema changes
    public JsonElement? Raw { get; set; }
}