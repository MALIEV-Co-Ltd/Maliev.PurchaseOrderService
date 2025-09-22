using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Stores references to documents uploaded to UploadService for purchase orders
/// </summary>
[Table("PurchaseOrderFiles")]
public class PurchaseOrderFile
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
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
    /// Status of virus scanning for this file
    /// </summary>
    [Required]
    [StringLength(50)]
    public string VirusScanStatus { get; set; } = "Pending";

    /// <summary>
    /// Date when virus scan was completed
    /// </summary>
    public DateTime? VirusScanCompletedAt { get; set; }

    /// <summary>
    /// MD5 hash of the file for integrity checking
    /// </summary>
    [StringLength(32)]
    public string? FileHash { get; set; }

    /// <summary>
    /// Flag indicating if file is available for download
    /// </summary>
    [Required]
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Download count for analytics
    /// </summary>
    [Required]
    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// Last download timestamp
    /// </summary>
    public DateTime? LastDownloadedAt { get; set; }

    /// <summary>
    /// User who last downloaded the file
    /// </summary>
    [StringLength(50)]
    public string? LastDownloadedBy { get; set; }

    /// <summary>
    /// External URL for file access (from UploadService)
    /// </summary>
    [StringLength(1000)]
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// Expiration date for signed URLs
    /// </summary>
    public DateTime? ExternalUrlExpiration { get; set; }

    /// <summary>
    /// Flag indicating if this is a system-generated file
    /// </summary>
    [Required]
    public bool IsSystemGenerated { get; set; } = false;

    /// <summary>
    /// User who last modified this file record
    /// </summary>
    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency control token
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation Properties

    /// <summary>
    /// Parent purchase order
    /// </summary>
    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}