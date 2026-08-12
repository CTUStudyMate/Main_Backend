using System.Security.Claims;
using MainBackend.Models;
using MainBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainBackend.Controllers;

[ApiController]
[Authorize(Roles = "lecturer,admin")]
[Route("api/curated-qa-exercise-dashboard")]
public class CuratedQaExerciseDashboardController : ControllerBase
{
    private readonly ICuratedQaExerciseDashboardService _dashboardService;

    public CuratedQaExerciseDashboardController(
        ICuratedQaExerciseDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("curated-qas")]
    public async Task<IActionResult> GetCuratedQas(
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var curatedQas = await _dashboardService.GetCuratedQasAsync(
            userId,
            cancellationToken);

        return Ok(curatedQas);
    }

    [HttpGet("exercises")]
    public async Task<IActionResult> GetExercises(
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var exercises = await _dashboardService.GetExercisesAsync(
            userId,
            cancellationToken);

        return Ok(exercises);
    }

    [HttpPatch("curated-qas/{curatedQaId:int}/enabled")]
    public async Task<IActionResult> UpdateCuratedQaEnabled(
        int curatedQaId,
        [FromBody] UpdateCuratedQaEnabledRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var curatedQa = await _dashboardService.UpdateCuratedQaEnabledAsync(
            userId,
            curatedQaId,
            request.IsEnabled,
            cancellationToken);

        return Ok(curatedQa);
    }

    [HttpPatch("exercises/{questionItemId:guid}/enabled")]
    public async Task<IActionResult> UpdateExerciseEnabled(
        Guid questionItemId,
        [FromBody] UpdateExerciseEnabledRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var exercise = await _dashboardService.UpdateExerciseEnabledAsync(
            userId,
            questionItemId,
            request.IsEnabled,
            cancellationToken);

        return Ok(exercise);
    }

    [HttpGet("users")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetUsers(
        CancellationToken cancellationToken)
    {
        var users = await _dashboardService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("users/{userId:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUser(
        int userId,
        [FromBody] AdminUpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dashboardService.UpdateUserAsync(
            userId,
            request,
            cancellationToken);
        return Ok(user);
    }

    [HttpPatch("users/{userId:int}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUserStatus(
        int userId,
        [FromBody] AdminUpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dashboardService.UpdateUserStatusAsync(
            userId,
            request.AccountStatus,
            cancellationToken);
        return Ok(user);
    }

    [HttpGet("system-documents")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetSystemDocuments(
        CancellationToken cancellationToken)
    {
        var documents = await _dashboardService.GetSystemDocumentsAsync(
            cancellationToken);
        return Ok(documents);
    }

    [HttpPut("system-documents/{documentId:int}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateSystemDocument(
        int documentId,
        [FromBody] AdminUpdateSystemDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentTitle) ||
            string.IsNullOrWhiteSpace(request.FileUrl))
        {
            return BadRequest(new
            {
                message = "Document title and file URL are required."
            });
        }

        var document = await _dashboardService.UpdateSystemDocumentAsync(
            documentId,
            request,
            cancellationToken);
        return Ok(document);
    }

    [HttpPatch("system-documents/{documentId:int}/visibility")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateSystemDocumentVisibility(
        int documentId,
        [FromBody] AdminUpdateDocumentVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DocumentVisibility>(
                request.Visibility,
                true,
                out var visibility))
        {
            return BadRequest(new { message = "Invalid visibility value." });
        }

        var document =
            await _dashboardService.UpdateSystemDocumentVisibilityAsync(
                documentId,
                visibility,
                cancellationToken);
        return Ok(document);
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out userId);
    }
}
