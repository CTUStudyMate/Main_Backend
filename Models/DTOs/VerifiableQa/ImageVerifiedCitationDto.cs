using System.Text.Json.Serialization;

namespace MainBackend.Models.DTOs.VerifiableQa;

public class ImageVerifiedCitationDto
{
    [JsonPropertyName("mark_number")]
    public int MarkNumber { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "image";

    [JsonPropertyName("doc_id")]
    public required string DocId { get; set; }

    [JsonPropertyName("image_id")]
    public required string ImageId { get; set; }
}
