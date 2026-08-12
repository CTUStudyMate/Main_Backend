using MainBackend.Models;

namespace MainBackend.Services;

public interface ICuratedQaExerciseDashboardService
{
    Task<List<LecturerCuratedQaItem>> GetCuratedQasAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<LecturerExerciseItem>> GetExercisesAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<LecturerCuratedQaItem> UpdateCuratedQaEnabledAsync(
        int userId,
        int curatedQaId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    Task<LecturerExerciseItem> UpdateExerciseEnabledAsync(
        int userId,
        Guid questionItemId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    Task<List<AdminDashboardUserItem>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<AdminDashboardUserItem> UpdateUserAsync(
        int userId,
        AdminUpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardUserItem> UpdateUserStatusAsync(
        int userId,
        string accountStatus,
        CancellationToken cancellationToken = default);

    Task<List<AdminSystemDocumentItem>> GetSystemDocumentsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSystemDocumentItem> UpdateSystemDocumentAsync(
        int documentId,
        AdminUpdateSystemDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminSystemDocumentItem> UpdateSystemDocumentVisibilityAsync(
        int documentId,
        DocumentVisibility visibility,
        CancellationToken cancellationToken = default);
}
