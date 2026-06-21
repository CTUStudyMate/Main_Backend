using System.Text.Json;
namespace MainBackend.Models;
public class RagSegment
{
    public required string Role { get; set; }
    public required string Type { get; set; }
    public required string Segment { get; set; }

    public required List<RagCitation> Citations { get; set; }

    public required ProcessedCiteObj ProcessedCiteObj { get; set; }

    // fallback when schema changes
    public JsonElement? Raw { get; set; }
}