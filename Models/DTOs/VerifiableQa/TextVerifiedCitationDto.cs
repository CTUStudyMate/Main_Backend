using System.Text.Json.Serialization;

namespace MainBackend.Models.DTOs.VerifiableQa;

public class TextVerifiedCitationDto
{
    [JsonPropertyName("mark_number")]
    public int MarkNumber { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("doc_id")]
    public required string DocId { get; set; }

    [JsonPropertyName("evidences")]
    public required List<string> Evidences { get; set; }
}
