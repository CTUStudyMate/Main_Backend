namespace MainBackend.Models;
public class RagCitation
{
    public string Type { get; set; } = string.Empty;
    // source_text | img

    public string? Content { get; set; }

    public string? ImgId { get; set; }

    public List<ProcessedText>? ProcessedTexts { get; set; }

    public List<ProcessedInfo>? ProcessedInfo { get; set; }
}