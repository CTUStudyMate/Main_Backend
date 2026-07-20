using Microsoft.EntityFrameworkCore;
using MainBackend.Models;
using MainBackend.Models.BackgroundWorker;

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

    public async Task<PendingQa?> GetVerifiableQaByIdFromDB(
        int verifiableQaId,
        User lecturer)
    {
        var lecturerCourseIds = await _db.Users
            .Where(user => user.UserId == lecturer.UserId)
            .SelectMany(user => user.Courses)
            .Select(course => course.CourseId)
            .ToListAsync();

        var verifiableQa = await _db.VerifiableQas
            .Include(qa => qa.User)
                .ThenInclude(user => user!.Major)
            .Include(qa => qa.Courses)
            .Where(qa => qa.VerifiableQaId == verifiableQaId)
            .Where(qa => qa.Status == VerifiableQaStatus.Pending)
            .Where(qa =>
                !qa.Courses.Any() ||
                qa.Courses.Any(course => lecturerCourseIds.Contains(course.CourseId)))
            .FirstOrDefaultAsync();

        return verifiableQa == null ? null : MapToPendingQa(verifiableQa);
    }

    public async Task<PendingQa?> ApproveVerifiableQaAsync(
        int verifiableQaId,
        string approvedAnswer,
        User lecturer
    )
    {
        var lecturerCourseIds = await _db.Users
            .Where(user => user.UserId == lecturer.UserId)
            .SelectMany(user => user.Courses)
            .Select(course => course.CourseId)
            .ToListAsync();

        var verifiableQa = await _db.VerifiableQas
            .Include(qa => qa.User)
                .ThenInclude(user => user!.Major)
            .Include(qa => qa.Courses)
            .Where(qa => qa.VerifiableQaId == verifiableQaId)
            .Where(qa => qa.Status == VerifiableQaStatus.Pending)
            .Where(qa =>
                !qa.Courses.Any() ||
                qa.Courses.Any(course => lecturerCourseIds.Contains(course.CourseId)))
            .FirstOrDefaultAsync();

        if (verifiableQa == null)
        {
            return null;
        }

        verifiableQa.ApprovedAnswer = approvedAnswer;
        verifiableQa.Status = VerifiableQaStatus.Approved;
        var now = DateTime.UtcNow;
        verifiableQa.UpdatedAt = now;

        var backgroundJob = new BackgroundJob
        {
            JobId = Guid.NewGuid(),
            Type = BackgroundJobType.GenerateEmbedding,
            Status = BackgroundJobStatus.Pending,
            SourceEntityType = BackgroundJobSourceEntityType.VerifiableQa,
            VerifiableQaId = verifiableQa.VerifiableQaId,
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now
        };

        var backgroundJobLog = new BackgroundJobLog
        {
            BackgroundJobLogId = Guid.NewGuid(),
            JobId = backgroundJob.JobId,
            Level = BackgroundJobLogLevel.Information,
            EventType = BackgroundJobLogEventType.JobCreated,
            AttemptNumber = backgroundJob.AttemptCount,
            Message = $"Generate embedding job created for Verifiable QA {verifiableQa.VerifiableQaId}.",
            CreatedAt = now
        };

        _db.BackgroundJobs.Add(backgroundJob);
        _db.BackgroundJobLogs.Add(backgroundJobLog);

        await _db.SaveChangesAsync();

        return MapToPendingQa(verifiableQa);
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
