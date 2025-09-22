using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Purchase order file data transfer object for API operations
/// Stores references to documents uploaded to UploadService for purchase orders
/// </summary>
public class PurchaseOrderFileDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to parent PurchaseOrder
    /// </summary>
    [Required]
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Original filename of the uploaded document
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full object path in GCS bucket (for organized storage)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Type of document (CustomerPO, InternalApproval, Invoice, Reference, GeneratedPDF, Other)
    /// </summary>
    [Required]
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    [Required]
    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// User ID who uploaded the document
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    [Required]
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Optional description of the document
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;
}

/// <summary>
/// File upload request for purchase order files
/// </summary>
public class UploadFileRequest
{
    /// <summary>
    /// The file to upload
    /// </summary>
    [Required]
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// Type of document
    /// Note: GeneratedPDF documents are created automatically and cannot be uploaded manually
    /// </summary>
    [Required]
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Optional custom object path in GCS bucket
    /// </summary>
    [StringLength(500)]
    public string? ObjectName { get; set; }

    /// <summary>
    /// Optional description of the document
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// Response object for file upload operations
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// Unique identifier of the uploaded file
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Object name/path in storage
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Content type/MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Document type
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// User who uploaded the file
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Response for file download operations
/// </summary>
public class FileDownloadResponse
{
    /// <summary>
    /// Redirect URL to download the file
    /// </summary>
    [Required]
    [Url]
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Filename for download
    /// </summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type for proper browser handling
    /// </summary>
    [Required]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [Required]
    public long FileSize { get; set; }
}