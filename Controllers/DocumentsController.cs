using System.Security.Claims;
using System.Text;
using MainBackend.Models;
using MainBackend.Models.DTOs.Documents;
using MainBackend.Services;
using MainBackend.Services.RagEngine;
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
    private readonly DocumentRagClient _ragClient;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        AppDbContext db,
        DocumentStorageService storage,
        DocumentRagClient ragClient,
        ILogger<DocumentsController> logger)
    {
        _db = db;
        _storage = storage;
        _ragClient = ragClient;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    public async Task<IActionResult> UploadPdf(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
        => await UploadPdfForSource(
            request.File,
            request.Title,
            request.CourseIds,
            DocumentSourceType.User,
            cancellationToken);

    [HttpPost("system/upload")]
    [Authorize(Roles = "admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    public async Task<IActionResult> UploadSystemPdf(
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
        => await UploadPdfForSource(
            request.File,
            request.Title,
            request.CourseIds,
            DocumentSourceType.System,
            cancellationToken);

    private async Task<IActionResult> UploadPdfForSource(
        IFormFile file,
        string? title,
        List<int>? courseIds,
        DocumentSourceType sourceType,
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
        var requestedCourseIds = (courseIds ?? [])
           .Distinct()
           .ToArray();

        if (requestedCourseIds.Any(courseId => courseId <= 0))
        {
            return BadRequest(new
            {
                message = "Course ids must be positive integers."
            });
        }

        var selectedCourses = requestedCourseIds.Length == 0
            ? new List<Course>()
            : await _db.Courses
                .Where(course => requestedCourseIds.Contains(course.CourseId))
                .ToListAsync(cancellationToken);

        if (selectedCourses.Count != requestedCourseIds.Length)
        {
            return BadRequest(new
            {
                message = "One or more selected courses do not exist."
            });
        }
        var now = DateTime.UtcNow;
        var document = new Document
        {
            UserId = userId.Value,
            DocumentTitle = BuildTitle(title, file.FileName),
            Courses = selectedCourses,
            FileUrl = string.Empty,
            OriginalFileName = Path.GetFileName(file.FileName),
            ContentType = "application/pdf",
            FileSizeBytes = file.Length,
            SourceType = sourceType,
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
        document.ProcessingRunId = Guid.NewGuid();
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

        try
        {
            await _ragClient.QueueDocumentAsync(
                document.DocumentId,
                document.ProcessingRunId.Value,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Could not queue uploaded document {DocumentId} with the RAG engine.",
                document.DocumentId);
            document.ProcessingStatus = DocumentProcessingStatus.Failed;
            document.ProcessingMessage = "The processing service is temporarily unavailable.";
            document.ProcessingUpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
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

    [HttpGet]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var documents = await _db.Documents
            .Include(document => document.Courses)
            .Where(document => document.UserId == userId.Value &&
                               document.SourceType == DocumentSourceType.User)
            .OrderByDescending(document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(documents.Select(ToDocumentResponse));
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetSystemDocumentCourses(
        CancellationToken cancellationToken)
    {
        var courses = await _db.Documents
            .AsNoTracking()
            .Where(document =>
                document.SourceType == DocumentSourceType.System &&
                document.Visibility == DocumentVisibility.Active &&
                document.ProcessingStatus == DocumentProcessingStatus.Ready)
            .SelectMany(document => document.Courses.Select(course => new
            {
                course.CourseId,
                course.CourseCode,
                course.CourseName,
                document.DocumentId,
            }))
            .GroupBy(item => new
            {
                item.CourseId,
                item.CourseCode,
                item.CourseName,
            })
            .Select(group => new
            {
                group.Key.CourseId,
                group.Key.CourseCode,
                group.Key.CourseName,
                DocumentIds = group
                    .Select(item => item.DocumentId)
                    .Distinct()
                    .OrderBy(documentId => documentId)
                    .ToList(),
            })
            .OrderBy(course => course.CourseCode)
            .ToListAsync(cancellationToken);

        return Ok(courses);
    }

    [HttpGet("ready")]
    public async Task<IActionResult> GetReadyDocuments(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var documents = await _db.Documents
            .AsNoTracking()
            .Where(document =>
                document.UserId == userId.Value &&
                document.SourceType == DocumentSourceType.User &&
                document.Visibility == DocumentVisibility.Active &&
                document.ProcessingStatus == DocumentProcessingStatus.Ready)
            .OrderByDescending(document => document.CreatedAt)
            .Select(document => new
            {
                document.DocumentId,
                document.DocumentTitle,
            })
            .ToListAsync(cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{documentId:int}/file")]
    public async Task<IActionResult> GetDocumentFile(int documentId, CancellationToken cancellationToken)
    {
        var document = await FindViewableDocumentAsync(documentId, cancellationToken);
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

        return await _db.Documents.Include(document => document.Courses).FirstOrDefaultAsync(
            document => document.DocumentId == documentId &&
                        document.UserId == userId.Value &&
            document.SourceType == DocumentSourceType.User,
            cancellationToken);
    }

    private async Task<Document?> FindViewableDocumentAsync(
        int documentId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        var document = await _db.Documents.FirstOrDefaultAsync(
            item => item.DocumentId == documentId,
            cancellationToken);

        if (document is null)
        {
            return null;
        }

        if (document.SourceType == DocumentSourceType.User)
        {
            return document.UserId == userId ? document : null;
        }

        return document.SourceType == DocumentSourceType.System ? document : null;
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

        if (title.Any(char.IsControl) || HasUnpairedSurrogate(title))
        {
            throw new ArgumentException(
                "Document title contains unsupported control or Unicode characters.");
        }

        return title.Length <= 250 ? title : title[..250];
    }

    private static bool HasUnpairedSurrogate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                {
                    return true;
                }

                index++;
            }
            else if (char.IsLowSurrogate(value[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static object ToDocumentResponse(Document document) => new
    {
        document.DocumentId,
        document.DocumentTitle,
        document.FileUrl,
        document.OriginalFileName,
        document.FileSizeBytes,
        courses = document.Courses
            .OrderBy(course => course.CourseCode)
            .Select(course => new
            {
                course.CourseId,
                course.CourseCode,
                course.CourseName,
            }),
        processingStatus = document.ProcessingStatus.ToString().ToLowerInvariant(),
        document.ProcessingProgress,
        document.ProcessingMessage,
        document.ProcessingUpdatedAt,
        document.CreatedAt,
    };
}
