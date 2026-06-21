namespace MainBackend.Models;
public class CreateUserResponse
{
    public required string Username { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }

}