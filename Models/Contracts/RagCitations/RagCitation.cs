using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class RagCitation
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    // source_text | img

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("img_id")]
    public string? ImgId { get; set; }

    [JsonPropertyName("processed_texts")]
    public List<List<string>>? ProcessedTexts { get; set; }

    [JsonPropertyName("processed_info")]
    public List<List<string>>? ProcessedInfo { get; set; }
}