namespace MainBackend.Models;
public class ProcessedText
{
    public string DocId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class ProcessedInfo
{
    public string DocId { get; set; } = string.Empty;
    public string ImageId { get; set; } = string.Empty;
}