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
        var user = new User
        {
            Username = userRequestInfo.Username,
            Password = "",
            Email = userRequestInfo.Email,
            Name = userRequestInfo.Name,
            Role = userRequestInfo.Role,
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