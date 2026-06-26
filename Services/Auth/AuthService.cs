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

    public async Task<RegisterResult> Register(RegisterRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user != null)
        {
            return new RegisterResult
            {
                Auth = null,
                Response = new RegisterResponse
                {
                    Status = "user_exists"
                }
            };
        }

        var insertUser = new User
        {
            Username = request.Username,
            Password = "",
            Email = request.Email,
            Name = request.Name,
            Role = "student",
            AccountStatus = "active"
        };

        insertUser.Password = _passwordHasher.HashPassword(insertUser, request.Password);

        _db.Users.Add(insertUser);
        await _db.SaveChangesAsync();

        return new RegisterResult
        {
            Response = new RegisterResponse
            {
                Status = "success",
                User = new RegisteredUser
                {
                    Username = insertUser.Username,
                    Name = insertUser.Name,
                    Email = insertUser.Email,
                    Role = insertUser.Role,
                    AccountStatus = insertUser.AccountStatus
                }
            },

            Auth = new AuthResult
            {
                UserId = insertUser.UserId,
                Username = insertUser.Username,
                Name = insertUser.Name,
                Email = insertUser.Email,
                Role = insertUser.Role,
                AccountStatus = insertUser.AccountStatus
            }
        };
    }
}