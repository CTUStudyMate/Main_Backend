using System.Security.Cryptography;
using System.Text;
using MainBackend.Models;
using MainBackend.Models.DTOs.Documents;
using MainBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Controllers;

[ApiController]
[Route("internal/documents")]
public sealed class InternalDocumentsController : ControllerBase
{
    private const string ServiceTokenHeader = "X-Internal-Service-Token";

    private readonly AppDbContext _db;
    private readonly DocumentStorageService _storage;
    private readonly IConfiguration _configuration;

    public InternalDocumentsController(
        AppDbContext db,
        DocumentStorageService storage,
        IConfiguration configuration)
    {
        _db = db;
        _storage = storage;
        _configuration = configuration;
    }

    [HttpGet("{documentId:int}/content")]
    public async Task<IActionResult> DownloadForProcessing(int documentId, CancellationToken cancellationToken)
    {
        if (!HasValidServiceToken())
        {
            return Unauthorized();
        }

        var document = await FindUploadedDocumentAsync(documentId, cancellationToken);
        if (document is null || string.IsNullOrWhiteSpace(document.StorageKey))
        {
            return NotFound(new { message = "Document file not found." });
        }

        var path = _storage.GetFullPath(document.StorageKey);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "Document file not found." });
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, document.ContentType ?? "application/pdf", document.OriginalFileName, enableRangeProcessing: true);
    }

    [HttpPost("{documentId:int}/processing-status")]
    public async Task<IActionResult> UpdateProcessingStatus(
        int documentId,
        [FromBody] DocumentProcessingStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!HasValidServiceToken())
        {
            return Unauthorized();
        }

        if (request.ProcessingRunId == Guid.Empty)
        {
            return BadRequest(new { message = "A valid processing run id is required." });
        }

        if (!Enum.TryParse<DocumentProcessingStatus>(request.Status, true, out var status))
        {
            return BadRequest(new { message = "Unsupported processing status." });
        }

        var document = await FindUploadedDocumentAsync(documentId, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Document not found." });
        }

        if (document.ProcessingRunId.HasValue && document.ProcessingRunId != request.ProcessingRunId)
        {
            return Conflict(new { message = "This status belongs to an outdated processing run." });
        }

        document.ProcessingRunId = request.ProcessingRunId;
        document.ProcessingStatus = status;
        document.ProcessingProgress = request.Progress;
        document.ProcessingMessage = request.Message?.Trim();
        document.ProcessingUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            document.DocumentId,
            status = document.ProcessingStatus.ToString().ToLowerInvariant(),
            document.ProcessingProgress,
            document.ProcessingUpdatedAt,
        });
    }

    private async Task<Document?> FindUploadedDocumentAsync(int documentId, CancellationToken cancellationToken) =>
        await _db.Documents.FirstOrDefaultAsync(
            document => document.DocumentId == documentId &&
                        document.SourceType == DocumentSourceType.User,
            cancellationToken);

    private bool HasValidServiceToken()
    {
        var configuredToken = _configuration["InternalApi:ServiceToken"];
        var suppliedToken = Request.Headers[ServiceTokenHeader].ToString();

        if (string.IsNullOrEmpty(configuredToken) || string.IsNullOrEmpty(suppliedToken))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configuredToken),
            Encoding.UTF8.GetBytes(suppliedToken));
    }
}
