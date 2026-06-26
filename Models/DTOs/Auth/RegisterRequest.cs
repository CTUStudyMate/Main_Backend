namespace MainBackend.Models;
public class RegisterRequest
{
    public required string Username { get; set; } 
    public required string Password { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string Name { get; set; }
}