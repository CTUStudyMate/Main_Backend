using System.Text.Json;
using MainBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services;

public class CuratedQaExerciseDashboardService : ICuratedQaExerciseDashboardService
{
    private readonly AppDbContext _db;

    public CuratedQaExerciseDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LecturerCuratedQaItem>> GetCuratedQasAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetDashboardAccessAsync(
            userId,
            cancellationToken);

        if (!access.IsAdmin && access.CourseIds.Count == 0)
        {
            return [];
        }

        IQueryable<CuratedQa> curatedQaQuery = _db.CuratedQas
            .AsNoTracking()
            .Include(curatedQa => curatedQa.VerifiableQa)
                .ThenInclude(verifiableQa => verifiableQa.Courses);

        if (!access.IsAdmin)
        {
            curatedQaQuery = curatedQaQuery.Where(curatedQa =>
                curatedQa.VerifiableQa.Courses.Any(course =>
                    access.CourseIds.Contains(course.CourseId)));
        }

        var curatedQas = await curatedQaQuery
            .OrderByDescending(curatedQa => curatedQa.CreatedAt)
            .ThenByDescending(curatedQa => curatedQa.CuratedQaId)
            .ToListAsync(cancellationToken);

        var curatedQaIds = curatedQas
            .Select(curatedQa => curatedQa.CuratedQaId)
            .ToList();

        var exerciseStats = await _db.QuestionItems
            .AsNoTracking()
            .Where(questionItem => curatedQaIds.Contains(questionItem.CuratedQaId))
            .GroupBy(questionItem => questionItem.CuratedQaId)
            .Select(group => new
            {
                CuratedQaId = group.Key,
                Count = group.Count(),
                IsEnabled = group.Any(questionItem => questionItem.IsEnabled)
            })
            .ToDictionaryAsync(item => item.CuratedQaId, cancellationToken);

        return curatedQas
            .Select(curatedQa =>
            {
                exerciseStats.TryGetValue(curatedQa.CuratedQaId, out var stats);

                return new LecturerCuratedQaItem
                {
                    CuratedQaId = curatedQa.CuratedQaId,
                    VerifiableQaId = curatedQa.VerifiableQaId,
                    CuratedQuestion = curatedQa.CuratedQuestion,
                    CuratedAnswer = curatedQa.CuratedAnswer,
                    CreatedAt = curatedQa.CreatedAt,
                    ExerciseCount = stats?.Count ?? 0,
                    IsEnabled = stats?.IsEnabled ?? false,
                    Courses = MapCourses(
                        curatedQa.VerifiableQa.Courses,
                        access.IsAdmin ? null : access.CourseIds)
                };
            })
            .ToList();
    }

    public async Task<List<LecturerExerciseItem>> GetExercisesAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetDashboardAccessAsync(
            userId,
            cancellationToken);

        if (!access.IsAdmin && access.CourseIds.Count == 0)
        {
            return [];
        }

        IQueryable<QuestionItem> questionItemQuery = _db.QuestionItems
            .AsNoTracking()
            .Include(questionItem => questionItem.CuratedQa)
                .ThenInclude(curatedQa => curatedQa.VerifiableQa)
                    .ThenInclude(verifiableQa => verifiableQa.Courses);

        if (!access.IsAdmin)
        {
            questionItemQuery = questionItemQuery.Where(questionItem =>
                questionItem.CuratedQa.VerifiableQa.Courses.Any(course =>
                    access.CourseIds.Contains(course.CourseId)));
        }

        var questionItems = await questionItemQuery
            .OrderByDescending(questionItem => questionItem.CreatedAt)
            .ThenByDescending(questionItem => questionItem.QuestionItemId)
            .ToListAsync(cancellationToken);

        return questionItems
            .Select(questionItem => new LecturerExerciseItem
            {
                QuestionItemId = questionItem.QuestionItemId,
                CuratedQaId = questionItem.CuratedQaId,
                CuratedQuestion = questionItem.CuratedQa.CuratedQuestion,
                Type = JsonNamingPolicy.CamelCase.ConvertName(
                    questionItem.Type.ToString()),
                QuestionData = JsonSerializer.Deserialize<JsonElement>(
                    questionItem.QuestionData),
                IsEnabled = questionItem.IsEnabled,
                CreatedAt = questionItem.CreatedAt,
                Courses = MapCourses(
                    questionItem.CuratedQa.VerifiableQa.Courses,
                    access.IsAdmin ? null : access.CourseIds)
            })
            .ToList();
    }

    public async Task<List<AdminDashboardUserItem>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .AsNoTracking()
            .Include(user => user.Major)
            .Include(user => user.Courses)
            .OrderBy(user => user.Name)
            .ThenBy(user => user.UserId)
            .ToListAsync(cancellationToken);

        return users
            .Select(user => new AdminDashboardUserItem
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Name = user.Name,
                Role = user.Role.ToString().ToLower(),
                Cohort = user.Cohort,
                AccountStatus = user.AccountStatus,
                MajorId = user.MajorId,
                MajorName = user.Major != null
                    ? user.Major.MajorName
                    : null,
                Courses = user.Courses
                    .OrderBy(course => course.CourseCode)
                    .Select(course => new LecturerDashboardCourse
                    {
                        CourseId = course.CourseId,
                        CourseCode = course.CourseCode,
                        CourseName = course.CourseName
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<List<AdminSystemDocumentItem>> GetSystemDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        var documents = await _db.Documents
            .AsNoTracking()
            .Where(document =>
                document.SourceType == DocumentSourceType.System)
            .Include(document => document.User)
            .Include(document => document.Courses)
            .OrderByDescending(document => document.CreatedAt)
            .ThenByDescending(document => document.DocumentId)
            .ToListAsync(cancellationToken);

        return documents
            .Select(MapSystemDocument)
            .ToList();
    }

    public async Task<AdminSystemDocumentItem> UpdateSystemDocumentAsync(
        int documentId,
        AdminUpdateSystemDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .Include(item => item.User)
            .Include(item => item.Courses)
            .FirstOrDefaultAsync(
                item =>
                    item.DocumentId == documentId &&
                    item.SourceType == DocumentSourceType.System,
                cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("System document not found.");
        }

        var courseIds = request.CourseIds.Distinct().ToList();
        var courses = await _db.Courses
            .Where(course => courseIds.Contains(course.CourseId))
            .ToListAsync(cancellationToken);

        if (courses.Count != courseIds.Count)
        {
            throw new ArgumentException(
                "One or more selected courses do not exist.");
        }

        document.DocumentTitle = request.DocumentTitle.Trim();
        document.FileUrl = request.FileUrl.Trim();
        document.Courses.Clear();
        foreach (var course in courses)
        {
            document.Courses.Add(course);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapSystemDocument(document);
    }

    public async Task<AdminSystemDocumentItem>
        UpdateSystemDocumentVisibilityAsync(
            int documentId,
            DocumentVisibility visibility,
            CancellationToken cancellationToken = default)
    {
        var document = await _db.Documents
            .Include(item => item.User)
            .Include(item => item.Courses)
            .FirstOrDefaultAsync(
                item =>
                    item.DocumentId == documentId &&
                    item.SourceType == DocumentSourceType.System,
                cancellationToken);

        if (document is null)
        {
            throw new KeyNotFoundException("System document not found.");
        }

        document.Visibility = visibility;
        await _db.SaveChangesAsync(cancellationToken);
        return MapSystemDocument(document);
    }

    private async Task<DashboardAccess> GetDashboardAccessAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Where(user => user.UserId == userId)
            .Select(user => new
            {
                user.Role,
                CourseIds = user.Courses.Select(course => course.CourseId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("Dashboard user not found.");
        }

        if (user.Role is not UserRole.Lecturer and not UserRole.Admin)
        {
            throw new UnauthorizedAccessException(
                "Only lecturers and administrators can access this dashboard.");
        }

        return new DashboardAccess(
            user.Role == UserRole.Admin,
            user.CourseIds.ToHashSet());
    }

    private static List<LecturerDashboardCourse> MapCourses(
        IEnumerable<Course> courses,
        IReadOnlySet<int>? allowedCourseIds)
    {
        return courses
            .Where(course =>
                allowedCourseIds is null ||
                allowedCourseIds.Contains(course.CourseId))
            .OrderBy(course => course.CourseCode)
            .Select(course => new LecturerDashboardCourse
            {
                CourseId = course.CourseId,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName
            })
            .ToList();
    }

    private static AdminSystemDocumentItem MapSystemDocument(
        Document document)
    {
        return new AdminSystemDocumentItem
        {
            DocumentId = document.DocumentId,
            DocumentTitle = document.DocumentTitle,
            FileUrl = document.FileUrl,
            Visibility = document.Visibility.ToString().ToLower(),
            CreatedAt = document.CreatedAt,
            OwnerUserId = document.UserId,
            OwnerName = document.User.Name,
            OwnerEmail = document.User.Email,
            Courses = MapCourses(document.Courses, null)
        };
    }

    private sealed record DashboardAccess(
        bool IsAdmin,
        HashSet<int> CourseIds);
}
