namespace MainBackend.Models;
public class GetChatsResponse
{
    public List<ChatResponse> Chats { get; set; } = new();
    public bool HasMore { get; set; }
}