using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MainBackend.Models;
using System.Security.Claims;
using MainBackend.Services;
namespace MainBackend.Controllers;

[ApiController]
[Route("api/verifiable-qa")]
public class VerifiableQaController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IVerifiableQaService _verifiableQaService;

    public VerifiableQaController(AppDbContext context, IVerifiableQaService verifiableQaService)
    {
        _context = context;
        _verifiableQaService = verifiableQaService;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetVerifiableQas()
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id")
        );

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (user.Role != UserRole.Lecturer)
        {
            return Forbid();
        }

        var verifiableQas = await _verifiableQaService.GetVerifiableQasFromDB(user);
        return Ok(verifiableQas);
    }

    [Authorize]
    [HttpGet("{verifiableQaId}")]
    public async Task<IActionResult> GetVerifiableQaById(int verifiableQaId)
    {
        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id")
        );

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (user.Role != UserRole.Lecturer)
        {
            return Forbid();
        }

        var verifiableQa = await _verifiableQaService
            .GetVerifiableQaByIdFromDB(verifiableQaId, user);

        if (verifiableQa == null)
        {
            return NotFound(new { message = "Verifiable QA not found" });
        }

        return Ok(verifiableQa);
    }

    [Authorize]
    [HttpPost("{verifiableQaId}/approve")]
    public async Task<IActionResult> ApproveVerifiableQa(
        int verifiableQaId,
        ApproveVerifiableQaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApprovedAnswer))
        {
            return BadRequest(new { message = "Approved answer is required" });
        }

        var userId = int.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing user id")
        );

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (user.Role != UserRole.Lecturer)
        {
            return Forbid();
        }

        var verifiableQa = await _verifiableQaService.ApproveVerifiableQaAsync(
            verifiableQaId,
            request.ApprovedAnswer,
            user);

        if (verifiableQa == null)
        {
            return NotFound(new { message = "Verifiable QA not found" });
        }

        return Ok(verifiableQa);
    }
}
