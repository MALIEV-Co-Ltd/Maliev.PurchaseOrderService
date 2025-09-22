namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of document upload operation
/// </summary>
public class DocumentUploadResult
{
    /// <summary>
    /// Whether the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The uploaded file information
    /// </summary>
    public PurchaseOrderFileDto? File { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// File ID assigned after successful upload
    /// </summary>
    public int? FileId { get; set; }

    /// <summary>
    /// File path in storage system
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File hash for integrity verification
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;
}