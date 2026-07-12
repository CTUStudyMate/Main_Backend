using Microsoft.AspNetCore.Identity;
using MainBackend.Models;

namespace MainBackend.Services;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    public UserService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }
    public async Task<User> CreateUserAsync(CreateUserRequest userRequestInfo)
    {
        if (!Enum.TryParse<UserRole>(userRequestInfo.Role, true, out var role))
        {
            throw new ArgumentException("Invalid role.");
        }

        var user = new User
        {
            Username = userRequestInfo.Username,
            Password = "",
            Email = userRequestInfo.Email,
            Name = userRequestInfo.Name,
            Role = role,
            AccountStatus = "active"
        };

        user.Password = _passwordHasher.HashPassword(
            user,
            userRequestInfo.Password
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

}