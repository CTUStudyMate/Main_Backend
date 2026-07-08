namespace MainBackend.Models;

public class RespondUserRequest
{
    public required Guid MessageId { get; set; }

    public required string Content { get; set; }

    public required string SenderType { get; set; } // "user" | "assistant"

    public required DateTime CreatedAt { get; set; }

    public required Guid ChatId { get; set; }
}