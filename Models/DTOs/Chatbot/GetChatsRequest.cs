namespace MainBackend.Models;
public class GetChatsRequest
{
    public int Limit { get; set; }

    public string? StartingAfter { get; set; } // updated after this chat id

    public string? EndingBefore { get; set; } // updated before this chat id
}