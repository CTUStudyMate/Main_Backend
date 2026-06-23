namespace MainBackend.Models;

public class MessageToFrontend
{
    public Guid MessageId { get; set; }
    public required string Content { get; set; }
     public List<RagSegment>? MessageSegments { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string SenderType { get; set; } // user | assistant

    // Foreign Key
    public Guid ChatId { get; set; }

}