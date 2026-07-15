using Microsoft.EntityFrameworkCore;
using MainBackend.Models;

namespace MainBackend.Services;

public class VerifiableQaService : IVerifiableQaService
{
    private readonly AppDbContext _db;

    public VerifiableQaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetVerifiableQasResponse> GetVerifiableQasFromDB(User lecturer)
    {
        try
        {
            var lecturerCourseIds = await _db.Users
                .Where(user => user.UserId == lecturer.UserId)
                .SelectMany(user => user.Courses)
                .Select(course => course.CourseId)
                .ToListAsync();

            var relatedQa = await _db.VerifiableQas
                .Include(qa => qa.User)
                    .ThenInclude(user => user!.Major)
                .Include(qa => qa.Courses)
                .Where(qa => qa.Status == VerifiableQaStatus.Pending)
                .Where(qa => qa.Courses.Any(course => lecturerCourseIds.Contains(course.CourseId)))
                .ToListAsync();

            var undefinedCourseQa = await _db.VerifiableQas
                .Include(qa => qa.User)
                    .ThenInclude(user => user!.Major)
                .Include(qa => qa.Courses)
                .Where(qa => qa.Status == VerifiableQaStatus.Pending)
                .Where(qa => !qa.Courses.Any())
                .ToListAsync();

            return new GetVerifiableQasResponse
            {
                Total = relatedQa.Count + undefinedCourseQa.Count,
                RelatedQa = relatedQa.Select(MapToPendingQa).ToList(),
                UndefinedCourseQa = undefinedCourseQa.Select(MapToPendingQa).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get verifiable QA list for lecturer {lecturer.UserId}.", ex);
        }
    }

    private static PendingQa MapToPendingQa(VerifiableQa qa)
    {
        return new PendingQa
        {
            VerifiableQaId = qa.VerifiableQaId,
            MessageId = qa.MessageId,
            UserId = qa.UserId,
            OriginalQuestion = qa.OriginalQuestion,
            RewrittenQuestion = qa.RewrittenQuestion,
            GeneratedAnswer = qa.GeneratedAnswer,
            Status = qa.Status,
            CreatedAt = qa.CreatedAt,
            User = qa.User == null
                ? null
                : new PendingQaUser
                {
                    UserId = qa.User.UserId,
                    MajorId = qa.User.MajorId,
                    MajorName = qa.User.Major?.MajorName,
                    Email = qa.User.Email,
                    Name = qa.User.Name,
                    Cohort = qa.User.Cohort,
                    AccountStatus = qa.User.AccountStatus
                },
            Courses = qa.Courses
                .Select(course => new PendingQaCourse
                {
                    CourseId = course.CourseId,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName
                })
                .ToList()
        };
    }
}
