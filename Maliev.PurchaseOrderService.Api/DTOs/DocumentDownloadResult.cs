namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of document download operation
/// </summary>
public class DocumentDownloadResult
{
    /// <summary>
    /// Whether the download was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// File stream for download
    /// </summary>
    public Stream? FileStream { get; set; }

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Error message if download failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// File metadata
    /// </summary>
    public PurchaseOrderFileDto? FileMetadata { get; set; }

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// ETag for caching
    /// </summary>
    public string? ETag { get; set; }
}