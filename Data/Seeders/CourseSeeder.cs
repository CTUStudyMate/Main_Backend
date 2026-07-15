using MainBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Data.Seeders;

public static class CourseSeeder
{
    public static async Task SeedCoursesAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        var courses = new[]
        {
            new Course { CourseCode = "CT114H", CourseName = "Introduction to Software Engineering" },
            new Course { CourseCode = "CT177H", CourseName = "Data Structures" },
            new Course { CourseCode = "CT108H", CourseName = "Object-Oriented Programming" },
            new Course { CourseCode = "CT109H", CourseName = "Analysis and Design of Algorithms" },
            new Course { CourseCode = "CT110H", CourseName = "Databases" },
            new Course { CourseCode = "CT112H", CourseName = "System Analysis and Design" },
            new Course { CourseCode = "CT582H", CourseName = "Software Analysis and Design" },
            new Course { CourseCode = "CT203H", CourseName = "Software Project Management" },
            new Course { CourseCode = "CT583H", CourseName = "Software Quality Assurance and Testing" },
            new Course { CourseCode = "CT244H", CourseName = "Software Maintenance" },
            new Course { CourseCode = "CT222HE", CourseName = "Application Development Foundations" },
            new Course { CourseCode = "CT214H", CourseName = "Web Programming" },
            new Course { CourseCode = "CT220HE", CourseName = "Cross-Platform Mobile Programming" },
            new Course { CourseCode = "CT218H", CourseName = "Application Programming with Java" },
            new Course { CourseCode = "CT219H", CourseName = "Application Programming with .NET" },
            new Course { CourseCode = "CT113H", CourseName = "NoSQL Databases" },
            new Course { CourseCode = "CT221H", CourseName = "Principles of Database Management Systems" },
            new Course { CourseCode = "CT287H", CourseName = "Model Checking" },
            new Course { CourseCode = "CT239HE", CourseName = "Professional Project" },
            new Course { CourseCode = "CT250HE", CourseName = "Capstone Project" }
        };

        foreach (var course in courses)
        {
            var exists = await db.Courses
                .AnyAsync(existingCourse => existingCourse.CourseCode == course.CourseCode);

            if (!exists)
            {
                db.Courses.Add(course);
            }
        }

        await db.SaveChangesAsync();
    }
}
