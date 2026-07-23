namespace MainBackend.Models;

public enum MessageSenderType
{
    User,
    Assistant
}

public class Message
{
    public Guid MessageId { get; set; }
    public int? UserId { get; set; }
    public Guid ChatId { get; set; }
    public required string Content { get; set; }
    public List<RagSegment> MessageSegments { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
    public MessageSenderType SenderType { get; set; }

    public int? VerifiableQaId { get; set; }
    public VerifiableQa? VerifiableQa { get; set; }
    public Chat Chat { get; set; } = null!;
    public User? User { get; set; }
}
