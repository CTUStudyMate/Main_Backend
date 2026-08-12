using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    public async Task<User> CreateUserAsync(
        CreateUserRequest userRequestInfo,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userRequestInfo.Username) ||
            string.IsNullOrWhiteSpace(userRequestInfo.Password) ||
            string.IsNullOrWhiteSpace(userRequestInfo.Email) ||
            string.IsNullOrWhiteSpace(userRequestInfo.Name))
        {
            throw new ArgumentException("Username, password, email, and name are required.");
        }

        if (!Enum.TryParse<UserRole>(userRequestInfo.Role, true, out var role))
        {
            throw new ArgumentException("Invalid role.");
        }

        var username = userRequestInfo.Username.Trim();
        var email = userRequestInfo.Email.Trim();
        if (await _context.Users.AnyAsync(
                user => user.Username == username,
                cancellationToken))
        {
            throw new ArgumentException("Username is already in use.");
        }

        if (await _context.Users.AnyAsync(
                user => user.Email == email,
                cancellationToken))
        {
            throw new ArgumentException("Email is already in use.");
        }

        var majorId = role is UserRole.Student or UserRole.Lecturer
            ? userRequestInfo.MajorId
            : null;
        if (majorId.HasValue && !await _context.Majors.AnyAsync(
                major => major.MajorId == majorId.Value,
                cancellationToken))
        {
            throw new ArgumentException("Selected major does not exist.");
        }

        var courseIds = role == UserRole.Lecturer
            ? userRequestInfo.CourseIds.Distinct().ToList()
            : [];
        var courses = await _context.Courses
            .Where(course => courseIds.Contains(course.CourseId))
            .ToListAsync(cancellationToken);
        if (courses.Count != courseIds.Count)
        {
            throw new ArgumentException("One or more selected courses do not exist.");
        }

        var user = new User
        {
            Username = username,
            Password = "",
            Email = email,
            Name = userRequestInfo.Name.Trim(),
            Role = role,
            AccountStatus = "active",
            MajorId = majorId,
            Cohort = role == UserRole.Student
                ? userRequestInfo.Cohort?.Trim() ?? null
                : null,
        };

        user.Password = _passwordHasher.HashPassword(
            user,
            userRequestInfo.Password
        );

        foreach (var course in courses)
        {
            user.Courses.Add(course);
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

}
