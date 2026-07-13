using MainBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Data.Seeders;

public static class MajorSeeder
{
    public static async Task SeedMajorsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        var majors = new[]
        {
            new Major { MajorCode = "7460108", MajorName = "Data Science" },
            new Major { MajorCode = "7480101", MajorName = "Computer Science" },
            new Major { MajorCode = "7480102", MajorName = "Computer Networks and Data Communications" },
            new Major { MajorCode = "7480102C", MajorName = "Computer Networks and Data Communications (High-Quality Program)" },
            new Major { MajorCode = "7480103", MajorName = "Software Engineering" },
            new Major { MajorCode = "7480103C", MajorName = "Software Engineering (High-Quality Program)" },
            new Major { MajorCode = "7480104", MajorName = "Information Systems" },
            new Major { MajorCode = "7480104C", MajorName = "Information Systems (High-Quality Program)" },
            new Major { MajorCode = "7480106", MajorName = "Computer Engineering (Semiconductor IC Design)" },
            new Major { MajorCode = "7480107", MajorName = "Artificial Intelligence" },
            new Major { MajorCode = "7480201", MajorName = "Information Technology" },
            new Major { MajorCode = "7480201C", MajorName = "Information Technology (High-Quality Program)" },
            new Major { MajorCode = "7480202", MajorName = "Information Security" }
        };

        foreach (var major in majors)
        {
            var exists = await db.Majors
                .AnyAsync(existingMajor => existingMajor.MajorCode == major.MajorCode);

            if (!exists)
            {
                db.Majors.Add(major);
            }
        }

        await db.SaveChangesAsync();
    }
}
