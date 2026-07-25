using System.Security.Claims;
using System.Text;
using MainBackend.Models;
using MainBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MainBackend.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private const long MaxUploadSizeBytes = 50 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly DocumentStorageService _storage;

    public DocumentsController(AppDbContext db, DocumentStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    public async Task<IActionResult> UploadPdf(
        [FromForm] IFormFile file,
        [FromForm] string? title,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "A non-empty PDF file is required." });
        }

        if (file.Length > MaxUploadSizeBytes ||
            !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase) ||
            !await HasPdfSignatureAsync(file, cancellationToken))
        {
            return BadRequest(new { message = "Only valid PDF files up to 50 MB are supported." });
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var now = DateTime.UtcNow;
        var document = new Document
        {
            UserId = userId.Value,
            DocumentTitle = BuildTitle(title, file.FileName),
            FileUrl = string.Empty,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = "application/pdf",
            FileSizeBytes = file.Length,
            SourceType = DocumentSourceType.User,
            Visibility = DocumentVisibility.Active,
            ProcessingStatus = DocumentProcessingStatus.Queued,
            ProcessingProgress = 0,
            ProcessingMessage = "Waiting to be processed.",
            ProcessingUpdatedAt = now,
            CreatedAt = now,
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        document.StorageKey = _storage.CreateStorageKey(userId.Value, document.DocumentId);
        document.FileUrl = $"/api/documents/{document.DocumentId}/file";
        var destinationPath = _storage.GetFullPath(document.StorageKey);

        try
        {
            _storage.EnsureDirectoryFor(document.StorageKey);
            await using var destination = new FileStream(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            await file.CopyToAsync(destination, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (System.IO.File.Exists(destinationPath))
            {
                System.IO.File.Delete(destinationPath);
            }

            _db.Documents.Remove(document);
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }

        return CreatedAtAction(
            nameof(GetDocument),
            new { documentId = document.DocumentId },
            ToDocumentResponse(document));
    }

    [HttpGet("{documentId:int}")]
    public async Task<IActionResult> GetDocument(int documentId, CancellationToken cancellationToken)
    {
        var document = await FindOwnedDocumentAsync(documentId, cancellationToken);
        return document is null ? NotFound(new { message = "Document not found." }) : Ok(ToDocumentResponse(document));
    }

    [HttpGet("{documentId:int}/file")]
    public async Task<IActionResult> GetDocumentFile(int documentId, CancellationToken cancellationToken)
    {
        var document = await FindOwnedDocumentAsync(documentId, cancellationToken);
        if (document is null)
        {
            return NotFound(new { message = "Document not found." });
        }

        return OpenDocumentFile(document);
    }

    private async Task<Document?> FindOwnedDocumentAsync(int documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await _db.Documents.FirstOrDefaultAsync(
            document => document.DocumentId == documentId &&
                        document.UserId == userId.Value &&
                        document.SourceType == DocumentSourceType.User,
            cancellationToken);
    }

    private IActionResult OpenDocumentFile(Document document)
    {
        if (string.IsNullOrWhiteSpace(document.StorageKey))
        {
            return NotFound(new { message = "The uploaded file is unavailable." });
        }

        var path = _storage.GetFullPath(document.StorageKey);
        if (!System.IO.File.Exists(path))
        {
            return NotFound(new { message = "The uploaded file is unavailable." });
        }

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, document.ContentType ?? "application/pdf", enableRangeProcessing: true);
    }

    private int? GetCurrentUserId() => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier),
        out var userId)
        ? userId
        : null;

    private static async Task<bool> HasPdfSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[5];
        var read = await stream.ReadAsync(header, cancellationToken);
        return read == header.Length && Encoding.ASCII.GetString(header) == "%PDF-";
    }

    private static string BuildTitle(string? suppliedTitle, string originalFilename)
    {
        var title = string.IsNullOrWhiteSpace(suppliedTitle)
            ? Path.GetFileNameWithoutExtension(originalFilename)
            : suppliedTitle.Trim();

        return title.Length <= 250 ? title : title[..250];
    }

    private static object ToDocumentResponse(Document document) => new
    {
        document.DocumentId,
        document.DocumentTitle,
        document.FileUrl,
        document.OriginalFileName,
        document.FileSizeBytes,
        processingStatus = document.ProcessingStatus.ToString().ToLowerInvariant(),
        document.ProcessingProgress,
        document.ProcessingMessage,
        document.ProcessingUpdatedAt,
        document.CreatedAt,
    };
}
