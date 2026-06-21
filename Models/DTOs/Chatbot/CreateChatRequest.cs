namespace MainBackend.Models;

public class CreateChatRequest
{
    public int UserId { get; set; }
    public string Title { get; set; } = "";
}