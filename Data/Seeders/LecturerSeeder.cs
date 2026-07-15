using MainBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Data.Seeders;

public static class LecturerSeeder
{
    public static async Task SeedLecturersAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        var lecturerSeeds = new[]
        {
            new
            {
                Username = "lecturer01",
                Email = "lecturer01@example.com",
                Name = "Lecturer 01",
                MajorCode = "7480103",
                CourseCodes = new[] { "CT114H", "CT582H", "CT203H", "CT583H" }
            },
            new
            {
                Username = "lecturer02",
                Email = "lecturer02@example.com",
                Name = "Lecturer 02",
                MajorCode = "7480101",
                CourseCodes = new[] { "CT177H", "CT109H", "CT287H" }
            },
            new
            {
                Username = "lecturer03",
                Email = "lecturer03@example.com",
                Name = "Lecturer 03",
                MajorCode = "7480104",
                CourseCodes = new[] { "CT110H", "CT112H", "CT221H" }
            }
        };

        foreach (var lecturerSeed in lecturerSeeds)
        {
            var existingUser = await db.Users
                .Include(user => user.Courses)
                .FirstOrDefaultAsync(user => user.Email == lecturerSeed.Email);

            var major = await db.Majors
                .FirstOrDefaultAsync(existingMajor => existingMajor.MajorCode == lecturerSeed.MajorCode);

            if (major == null)
            {
                continue;
            }

            var courses = await db.Courses
                .Where(course => lecturerSeed.CourseCodes.Contains(course.CourseCode))
                .ToListAsync();

            if (existingUser != null)
            {
                existingUser.MajorId = major.MajorId;
                existingUser.Role = UserRole.Lecturer;
                existingUser.AccountStatus = "active";

                foreach (var course in courses)
                {
                    if (!existingUser.Courses.Any(existingCourse => existingCourse.CourseId == course.CourseId))
                    {
                        existingUser.Courses.Add(course);
                    }
                }

                continue;
            }

            var user = new User
            {
                Username = lecturerSeed.Username,
                Password = string.Empty,
                Email = lecturerSeed.Email,
                Name = lecturerSeed.Name,
                Role = UserRole.Lecturer,
                AccountStatus = "active",
                MajorId = major.MajorId
            };

            user.Password = passwordHasher.HashPassword(user, "123");

            foreach (var course in courses)
            {
                user.Courses.Add(course);
            }

            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }
}
