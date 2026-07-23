using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class CuratedQaBatchResponse
{
    [JsonPropertyName("curated_qas")]
    public List<CuratedQaResponse> CuratedQas { get; set; } = [];
}

public class CuratedQaResponse
{
    [JsonPropertyName("question")]
    public required string Question { get; set; }

    [JsonPropertyName("answer")]
    public required string Answer { get; set; }
}
