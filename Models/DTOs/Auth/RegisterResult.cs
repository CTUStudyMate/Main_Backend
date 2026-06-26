namespace MainBackend.Models;
public class RegisterResult
{
    public AuthResult? Auth { get; set; }
    public required RegisterResponse Response { get; set; }
}