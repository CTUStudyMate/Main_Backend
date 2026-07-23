using System.Text.Json;
using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class VerifiedAnswerDto
{
    [JsonPropertyName("edited_answer")]
    public required string EditedAnswer { get; set; }

    [JsonPropertyName("citations")]
    public required Dictionary<string, JsonElement> Citations { get; set; }

    [JsonPropertyName("citation_map")]
    public required Dictionary<string, int> CitationMap { get; set; }
}
