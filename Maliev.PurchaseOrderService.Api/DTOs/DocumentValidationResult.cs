namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of document validation
/// </summary>
public class DocumentValidationResult
{
    /// <summary>
    /// Whether the file is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Detected file type
    /// </summary>
    public string? DetectedFileType { get; set; }

    /// <summary>
    /// Whether the file type is allowed
    /// </summary>
    public bool IsFileTypeAllowed { get; set; }

    /// <summary>
    /// Whether the file size is within limits
    /// </summary>
    public bool IsSizeValid { get; set; }

    /// <summary>
    /// Maximum allowed file size in bytes
    /// </summary>
    public long MaxFileSize { get; set; }

    /// <summary>
    /// File size provided for validation
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Allowed file extensions
    /// </summary>
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
}