namespace MainBackend.Models;

public class Chat
{
    public Guid ChatId { get; set; }
    public required string ChatTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}