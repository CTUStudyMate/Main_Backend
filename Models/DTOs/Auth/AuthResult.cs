namespace MainBackend.Models;

public class AuthResult
{
    public required int UserId { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string Name { get; set; }

    public required string AccountStatus { get; set; }
    public required string Username { get; set; }
}