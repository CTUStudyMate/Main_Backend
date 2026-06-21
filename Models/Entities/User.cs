namespace MainBackend.Models;

public class User
{
    public int UserId { get; set; }
    public required string Username { get; set; } 
    public required string Password { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public required string AccountStatus { get; set; }

    // Navigation property
    public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}