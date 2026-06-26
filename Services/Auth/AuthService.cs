using MainBackend.Services;
using MainBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult?> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.Password,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return null;

        return new AuthResult
        {
            Email = user.Email,
            Role = user.Role,
            Name = user.Name,
            UserId = user.UserId,
            Username = user.Username,
            AccountStatus = user.AccountStatus
        };
    }
}