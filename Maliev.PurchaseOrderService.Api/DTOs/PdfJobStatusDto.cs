namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Status information for PDF generation job
/// </summary>
public class PdfJobStatusDto
{
    /// <summary>
    /// The job ID
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// Job status (Pending, Processing, Completed, Failed, Cancelled)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Estimated completion time
    /// </summary>
    public DateTime? EstimatedCompletionTime { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the job was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Job completion time
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}