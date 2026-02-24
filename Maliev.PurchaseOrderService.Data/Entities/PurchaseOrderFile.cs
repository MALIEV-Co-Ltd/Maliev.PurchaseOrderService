using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Stores references to documents uploaded to UploadService for purchase orders
/// </summary>
public class PurchaseOrderFile
{
    /// <summary>
    /// Unique identifier for the file
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the purchase order
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Original filename of the uploaded document
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Full object path in GCS bucket (for organized storage)
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// Type of the document
    /// </summary>
    public DocumentType DocumentType { get; set; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// User ID who uploaded the file
    /// </summary>
    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the file was uploaded
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Description of the file
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Navigation property to the parent PurchaseOrder
    /// </summary>
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
