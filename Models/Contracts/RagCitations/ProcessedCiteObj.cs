using System.Text.Json.Serialization;

namespace MainBackend.Models;

public class ProcessedCiteObj
{
    [JsonPropertyName("texts")]
    public Dictionary<string, List<string>> Texts { get; set; }
        = new();

    [JsonPropertyName("images")]
    public Dictionary<string, List<string>> Images { get; set; }
        = new();
}