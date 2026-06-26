namespace MainBackend.Models;

public class ChatResponse
{
    public Guid ChatId { get; set; }
    public string ChatTitle { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
}