using MainBackend.Models;
namespace MainBackend.Services;
public interface IAuthService
{
    Task<AuthResult?> Login(LoginRequest request);
    Task<RegisterResult> Register(RegisterRequest request);
}