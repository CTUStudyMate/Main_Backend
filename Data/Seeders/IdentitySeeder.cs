using MainBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Data.Seeders;

public static class IdentitySeeder
{
    public static async Task SeedAdminAndLecturerAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        await EnsureUserAsync(
            db,
            passwordHasher,
            username: "admin",
            email: "admin@example.com",
            name: "System Admin",
            password: "Admin@123456",
            role: UserRole.Admin
        );

        await EnsureUserAsync(
            db,
            passwordHasher,
            username: "lecturer",
            email: "lecturer@example.com",
            name: "Default Lecturer",
            password: "Lecturer@123456",
            role: UserRole.Lecturer
        );

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        string username,
        string email,
        string name,
        string password,
        UserRole role)
    {
        var existingUser = await db.Users
            .FirstOrDefaultAsync(user => user.Email == email);

        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            Username = username,
            Password = string.Empty,
            Email = email,
            Name = name,
            Role = role,
            AccountStatus = "active"
        };

        user.Password = passwordHasher.HashPassword(user, password);

        db.Users.Add(user);
    }
}
