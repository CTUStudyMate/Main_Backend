using System.Security.Claims;
using MainBackend.Models;
using MainBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/review-dashboard")]
public class ReviewDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ReviewService _reviewService;

    public ReviewDashboardController(
        AppDbContext context,
        ReviewService reviewService)
    {
        _context = context;
        _reviewService = reviewService;
    }

    /// <summary>
    /// Gets the authenticated user's review sessions, newest first.
    /// </summary>
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var reviews = await _context.ReviewSessions
            .AsNoTracking()
            .Where(review => review.UserId == userId)
            .OrderByDescending(review => review.CreatedAt)
            .ThenByDescending(review => review.ReviewSessionId)
            .Select(review => new
            {
                review.ReviewSessionId,
                review.CourseId,
                CourseCode = review.Course == null ? null : review.Course.CourseCode,
                CourseName = review.Course == null ? null : review.Course.CourseName,
                review.TotalQuestions,
                review.CorrectAnswers,
                review.CreatedAt,
                review.CompletedAt,
                review.IsCompleted
            })
            .ToListAsync(cancellationToken);

        return Ok(reviews);
    }

    /// <summary>
    /// Gets one review session, including its questions and any saved answers.
    /// </summary>
    [HttpGet("reviews/{reviewSessionId:guid}")]
    public async Task<IActionResult> GetReview(
        Guid reviewSessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var review = await _reviewService.GetReviewAsync(
            userId,
            reviewSessionId,
            cancellationToken);

        return Ok(review);
    }

    /// <summary>
    /// Gets the result of a completed review session.
    /// </summary>
    [HttpGet("reviews/{reviewSessionId:guid}/results")]
    public async Task<IActionResult> GetReviewResults(
        Guid reviewSessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var review = await _reviewService.GetReviewAsync(
            userId,
            reviewSessionId,
            cancellationToken);

        if (!review.IsCompleted)
        {
            return BadRequest(new { message = "This review has not been submitted yet." });
        }

        return Ok(review);
    }

    /// <summary>
    /// Creates a new review session using the same questions as an existing session.
    /// </summary>
    [HttpPost("reviews/{reviewSessionId:guid}/retake")]
    public async Task<IActionResult> RetakeReview(
        Guid reviewSessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var review = await _reviewService.RetakeReviewAsync(
            userId,
            reviewSessionId,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, review);
    }

    /// <summary>
    /// Creates a review session for the authenticated user.
    /// </summary>
    [HttpPost("reviews")]
    public async Task<IActionResult> RequestReviewCreation(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var reviewSession = await _reviewService.RequestReviewCreationAsync(
            userId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, reviewSession);
    }

    /// <summary>
    /// Saves the authenticated user's in-progress review answers.
    /// </summary>
    [HttpPost("reviews/pause")]
    public async Task<IActionResult> PauseReview(
        [FromBody] PauseReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        await _reviewService.PauseReviewAsync(
            userId,
            request,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Grades and completes the authenticated user's review session.
    /// </summary>
    [HttpPost("reviews/submit")]
    public async Task<IActionResult> SubmitReview(
        [FromBody] SubmitReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _reviewService.SubmitReviewAsync(
            userId,
            request,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets every question answered by the authenticated user across all review sessions.
    /// </summary>
    [HttpGet("answered-questions")]
    public async Task<IActionResult> GetAnsweredQuestions(CancellationToken cancellationToken)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized();
        }

        var answeredQuestions = await _context.QuestionResponses
            .AsNoTracking()
            .Where(response => response.ReviewSession.UserId == userId)
            .OrderByDescending(response => response.ReviewSession.CreatedAt)
            .ThenByDescending(response => response.QuestionResponseId)
            .Select(response => new
            {
                response.QuestionResponseId,
                response.ReviewSessionId,
                ReviewCreatedAt = response.ReviewSession.CreatedAt,
                response.QuestionItemId,
                QuestionType = response.QuestionItem.Type,
                response.QuestionItem.QuestionData,
                response.StudentAnswer,
                response.IsCorrect
            })
            .ToListAsync(cancellationToken);

        return Ok(answeredQuestions);
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out userId);
    }
}
