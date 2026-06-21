namespace MainBackend.Models;
public class Message
{
    public Guid MessageId { get; set; }
    public required string Content { get; set; }
    public string? MessageSegments {get; set; }
    public DateTime CreatedAt { get; set; }
    public required string SenderType { get; set; } // user | assistant

    // Foreign Key
    public Guid ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}