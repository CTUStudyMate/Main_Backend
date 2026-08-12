using System.Text.Json;

namespace MainBackend.Models;

public class LecturerDashboardCourse
{
    public int CourseId { get; set; }
    public required string CourseCode { get; set; }
    public required string CourseName { get; set; }
}

public class LecturerCuratedQaItem
{
    public int CuratedQaId { get; set; }
    public int VerifiableQaId { get; set; }
    public required string CuratedQuestion { get; set; }
    public required string CuratedAnswer { get; set; }
    public required string VerifiableQuestion { get; set; }
    public string? ApprovedAnswer { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int ExerciseCount { get; set; }
    public bool IsEnabled { get; set; }
    public List<LecturerDashboardCourse> Courses { get; set; } = [];
}

public class LecturerExerciseItem
{
    public Guid QuestionItemId { get; set; }
    public int CuratedQaId { get; set; }
    public required string CuratedQuestion { get; set; }
    public required string Type { get; set; }
    public JsonElement QuestionData { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<LecturerDashboardCourse> Courses { get; set; } = [];
}

public class AdminDashboardUserItem
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public string? Cohort { get; set; }
    public required string AccountStatus { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public List<LecturerDashboardCourse> Courses { get; set; } = [];
}

public class AdminSystemDocumentItem
{
    public int DocumentId { get; set; }
    public required string DocumentTitle { get; set; }
    public required string FileUrl { get; set; }
    public required string Visibility { get; set; }
    public required string ProcessingStatus { get; set; }
    public int ProcessingProgress { get; set; }
    public string? ProcessingMessage { get; set; }
    public DateTime? ProcessingUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OwnerUserId { get; set; }
    public required string OwnerName { get; set; }
    public required string OwnerEmail { get; set; }
    public List<LecturerDashboardCourse> Courses { get; set; } = [];
}

public class AdminUpdateSystemDocumentRequest
{
    public required string DocumentTitle { get; set; }
    public required string FileUrl { get; set; }
    public List<int> CourseIds { get; set; } = [];
}

public class AdminUpdateDocumentVisibilityRequest
{
    public required string Visibility { get; set; }
}

public class AdminUpdateUserRequest
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public string? NewPassword { get; set; }
    public int? MajorId { get; set; }
    public List<int> CourseIds { get; set; } = [];
}

public class AdminUpdateUserStatusRequest
{
    public required string AccountStatus { get; set; }
}

public class UpdateCuratedQaEnabledRequest
{
    public bool IsEnabled { get; set; }
}

public class UpdateExerciseEnabledRequest
{
    public bool IsEnabled { get; set; }
}
