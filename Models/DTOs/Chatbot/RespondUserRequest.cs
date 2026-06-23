namespace MainBackend.Models;

public class RespondUserRequest
{
    public required Guid ChatId { get; set; }
    public required string Query { get; set; }
}