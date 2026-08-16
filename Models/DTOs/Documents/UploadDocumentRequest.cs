using Microsoft.AspNetCore.Http;

namespace MainBackend.Models.DTOs.Documents;

public sealed class UploadDocumentRequest
{
    public IFormFile File { get; init; } = default!;
    public string? Title { get; init; }
    public List<int>? CourseIds { get; init; }
}
