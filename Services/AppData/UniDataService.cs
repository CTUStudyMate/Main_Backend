using MainBackend.Models;
using Microsoft.EntityFrameworkCore;


namespace MainBackend.Services;

public class UniDataService : IAppDataService
{
    private readonly AppDbContext _db;

    public UniDataService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Major>> GetMajors()
    {
        return await _db.Majors
            .OrderBy(major => major.MajorCode)
            .ToListAsync();
    }

    public async Task<List<Course>> GetCourses()
    {
        return await _db.Courses
            .AsNoTracking()
            .OrderBy(course => course.CourseCode)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentById(int documentId)
    {
        return await _db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(document => document.DocumentId == documentId);
    }
}
