using System.Text.Json;
using MainBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Services;

public class CuratedQaExerciseDashboardService : ICuratedQaExerciseDashboardService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public CuratedQaExerciseDashboardService(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
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
                .ThenInclude(verifiableQa => verifiableQa.Courses)
            .Include(curatedQa => curatedQa.VerifiableQa)
                .ThenInclude(verifiableQa => verifiableQa.ApprovedByUser);

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

        var curatedQaIds = curatedQas.Select(curatedQa => curatedQa.CuratedQaId).ToList();
        var exerciseCounts = await _db.QuestionItems
            .AsNoTracking()
            .Where(questionItem => curatedQaIds.Contains(questionItem.CuratedQaId))
            .GroupBy(questionItem => questionItem.CuratedQaId)
            .Select(group => new
            {
                CuratedQaId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(item => item.CuratedQaId, cancellationToken);

        return curatedQas
            .Select(curatedQa =>
            {
                exerciseCounts.TryGetValue(curatedQa.CuratedQaId, out var stats);

                return new LecturerCuratedQaItem
                {
                    CuratedQaId = curatedQa.CuratedQaId,
                    VerifiableQaId = curatedQa.VerifiableQaId,
                    CuratedQuestion = curatedQa.CuratedQuestion,
                    CuratedAnswer = curatedQa.CuratedAnswer,
                    VerifiableQuestion = curatedQa.VerifiableQa.RewrittenQuestion
                        ?? curatedQa.VerifiableQa.OriginalQuestion,
                    ApprovedAnswer = curatedQa.VerifiableQa.ApprovedAnswer,
                    ApprovedByName = curatedQa.VerifiableQa.ApprovedByUser?.Name,
                    CreatedAt = curatedQa.CreatedAt,
                    ExerciseCount = stats?.Count ?? 0,
                    IsEnabled = curatedQa.IsEnabled,
                    Courses = MapCourses(
                        curatedQa.VerifiableQa.Courses,
                        access.IsAdmin ? null : access.CourseIds)
                };
            })
            .ToList();
    }

    public async Task<LecturerCuratedQaItem> UpdateCuratedQaEnabledAsync(
        int userId,
        int curatedQaId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var access = await GetDashboardAccessAsync(userId, cancellationToken);
        var curatedQa = await _db.CuratedQas
            .Include(item => item.VerifiableQa)
                .ThenInclude(item => item.Courses)
            .Include(item => item.VerifiableQa)
                .ThenInclude(item => item.ApprovedByUser)
            .FirstOrDefaultAsync(
                item => item.CuratedQaId == curatedQaId,
                cancellationToken);

        if (curatedQa is null)
        {
            throw new KeyNotFoundException("Curated QA not found.");
        }

        if (!access.IsAdmin && !curatedQa.VerifiableQa.Courses.Any(
                course => access.CourseIds.Contains(course.CourseId)))
        {
            throw new UnauthorizedAccessException(
                "You are not assigned to a course containing this curated QA.");
        }

        curatedQa.IsEnabled = isEnabled;
        var questionItems = await _db.QuestionItems
            .Where(item => item.CuratedQaId == curatedQaId)
            .ToListAsync(cancellationToken);

        foreach (var questionItem in questionItems)
        {
            questionItem.IsEnabled = isEnabled;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var exerciseCount = await _db.QuestionItems
            .CountAsync(
                item => item.CuratedQaId == curatedQaId,
                cancellationToken);

        return new LecturerCuratedQaItem
        {
            CuratedQaId = curatedQa.CuratedQaId,
            VerifiableQaId = curatedQa.VerifiableQaId,
            CuratedQuestion = curatedQa.CuratedQuestion,
            CuratedAnswer = curatedQa.CuratedAnswer,
            VerifiableQuestion = curatedQa.VerifiableQa.RewrittenQuestion
                ?? curatedQa.VerifiableQa.OriginalQuestion,
            ApprovedAnswer = curatedQa.VerifiableQa.ApprovedAnswer,
            ApprovedByName = curatedQa.VerifiableQa.ApprovedByUser?.Name,
            CreatedAt = curatedQa.CreatedAt,
            ExerciseCount = exerciseCount,
            IsEnabled = curatedQa.IsEnabled,
            Courses = MapCourses(
                curatedQa.VerifiableQa.Courses,
                access.IsAdmin ? null : access.CourseIds)
        };
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

    public async Task<LecturerExerciseItem> UpdateExerciseEnabledAsync(
        int userId,
        Guid questionItemId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var access = await GetDashboardAccessAsync(userId, cancellationToken);
        var questionItem = await _db.QuestionItems
            .Include(item => item.CuratedQa)
                .ThenInclude(item => item.VerifiableQa)
                    .ThenInclude(item => item.Courses)
            .FirstOrDefaultAsync(
                item => item.QuestionItemId == questionItemId,
                cancellationToken);

        if (questionItem is null)
        {
            throw new KeyNotFoundException("Exercise not found.");
        }

        if (!access.IsAdmin && !questionItem.CuratedQa.VerifiableQa.Courses.Any(
                course => access.CourseIds.Contains(course.CourseId)))
        {
            throw new UnauthorizedAccessException(
                "You are not assigned to a course containing this exercise.");
        }

        questionItem.IsEnabled = isEnabled;
        await _db.SaveChangesAsync(cancellationToken);

        return new LecturerExerciseItem
        {
            QuestionItemId = questionItem.QuestionItemId,
            CuratedQaId = questionItem.CuratedQaId,
            CuratedQuestion = questionItem.CuratedQa.CuratedQuestion,
            Type = JsonNamingPolicy.CamelCase.ConvertName(questionItem.Type.ToString()),
            QuestionData = JsonSerializer.Deserialize<JsonElement>(questionItem.QuestionData),
            IsEnabled = questionItem.IsEnabled,
            CreatedAt = questionItem.CreatedAt,
            Courses = MapCourses(
                questionItem.CuratedQa.VerifiableQa.Courses,
                access.IsAdmin ? null : access.CourseIds)
        };
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

    public async Task<AdminDashboardUserItem> UpdateUserAsync(
        int userId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Name and email are required.");
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            throw new ArgumentException("Invalid role.");
        }

        var user = await _db.Users
            .Include(item => item.Major)
            .Include(item => item.Courses)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var email = request.Email.Trim();
        var emailInUse = await _db.Users.AnyAsync(
            item => item.UserId != userId && item.Email == email,
            cancellationToken);
        if (emailInUse)
        {
            throw new ArgumentException("Email is already in use.");
        }

        if ((role is UserRole.Student or UserRole.Lecturer) &&
            request.MajorId.HasValue)
        {
            var majorExists = await _db.Majors.AnyAsync(
                item => item.MajorId == request.MajorId.Value,
                cancellationToken);
            if (!majorExists)
            {
                throw new ArgumentException("Selected major does not exist.");
            }
        }

        var courseIds = role == UserRole.Lecturer
            ? request.CourseIds.Distinct().ToList()
            : [];
        var courses = await _db.Courses
            .Where(course => courseIds.Contains(course.CourseId))
            .ToListAsync(cancellationToken);
        if (courses.Count != courseIds.Count)
        {
            throw new ArgumentException("One or more selected courses do not exist.");
        }

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Role = role;
        user.MajorId = role is UserRole.Student or UserRole.Lecturer
            ? request.MajorId
            : null;
        user.Courses.Clear();
        foreach (var course in courses)
        {
            user.Courses.Add(course);
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.Password = _passwordHasher.HashPassword(user, request.NewPassword);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<AdminDashboardUserItem> UpdateUserStatusAsync(
        int userId,
        string accountStatus,
        CancellationToken cancellationToken = default)
    {
        var normalizedStatus = accountStatus.Trim().ToLowerInvariant();
        if (normalizedStatus is not "active" and not "suspended")
        {
            throw new ArgumentException("Account status must be active or suspended.");
        }

        var user = await _db.Users
            .Include(item => item.Major)
            .Include(item => item.Courses)
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.AccountStatus = normalizedStatus;
        await _db.SaveChangesAsync(cancellationToken);
        return MapUser(user);
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
            ProcessingStatus = document.ProcessingStatus.ToString().ToLower(),
            ProcessingProgress = document.ProcessingProgress,
            ProcessingMessage = document.ProcessingMessage,
            ProcessingUpdatedAt = document.ProcessingUpdatedAt,
            CreatedAt = document.CreatedAt,
            OwnerUserId = document.UserId,
            OwnerName = document.User.Name,
            OwnerEmail = document.User.Email,
            Courses = MapCourses(document.Courses, null)
        };
    }

    private static AdminDashboardUserItem MapUser(User user)
    {
        return new AdminDashboardUserItem
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString().ToLower(),
            Cohort = user.Cohort,
            AccountStatus = user.AccountStatus,
            MajorId = user.MajorId,
            MajorName = user.Major?.MajorName,
            Courses = MapCourses(user.Courses, null)
        };
    }

    private sealed record DashboardAccess(
        bool IsAdmin,
        HashSet<int> CourseIds);
}
