using System.ComponentModel.DataAnnotations;

namespace MainBackend.Models.DTOs.Documents;

public sealed class DocumentProcessingStatusUpdateRequest
{
    [Required]
    public Guid ProcessingRunId { get; init; }

    [Required]
    public string Status { get; init; } = string.Empty;

    [Range(0, 100)]
    public int Progress { get; init; }

    [StringLength(500)]
    public string? Message { get; init; }
}
