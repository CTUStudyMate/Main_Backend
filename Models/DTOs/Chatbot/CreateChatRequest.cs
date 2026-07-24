namespace MainBackend.Models;

public class CreateChatRequest
{
    public Guid ChatId { get; set; }
    public string? TitleSource { get; set; }
}
