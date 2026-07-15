using MainBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Data.Seeders;

public static class DocumentSeeder
{
    public static async Task SeedDocumentsAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();

        var existingDocument = await db.Documents
            .Include(document => document.Courses)
            .FirstOrDefaultAsync(document => document.DocumentId == 1);

        if (existingDocument != null)
        {
            return;
        }

        var document = new Document
        {
            DocumentId = 1,
            UserId = 3,
            DocumentTitle = "Software Engineering Theory and Practice 4th Edition",
            FileUrl = "https://example.com/software-engineering-theory-and-practice-4th-edition.pdf",
            SourceType = DocumentSourceType.System,
            Visibility = DocumentVisibility.Active,
            CreatedAt = DateTime.UtcNow
        };

        var course = await db.Courses
            .FirstOrDefaultAsync(existingCourse => existingCourse.CourseCode == "CT114H");

        if (course != null)
        {
            document.Courses.Add(course);
        }

        db.Documents.Add(document);
        await db.SaveChangesAsync();
    }
}
