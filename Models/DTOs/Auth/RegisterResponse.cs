namespace MainBackend.Models;

public class RegisteredUser
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public required string AccountStatus { get; set; }
}
public class RegisterResponse
{
    public required string Status { get; set; }
    public RegisteredUser? User { get; set; }

}