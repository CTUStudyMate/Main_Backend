using MainBackend.Models;

public interface IJwtService
{
    string GenerateToken(AuthResult user);
}