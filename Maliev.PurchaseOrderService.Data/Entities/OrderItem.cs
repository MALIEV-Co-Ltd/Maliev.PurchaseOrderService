using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Individual line items derived from OrderService/QuotationService for the referenced order.
/// Read-only entity populated from external services.
/// </summary>
[Table("OrderItems")]
public class OrderItem
{
    /// <summary>
    /// Local unique identifier for caching
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Reference to parent PurchaseOrder
    /// </summary>
    [Required]
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Reference to the original item in OrderService
    /// </summary>
    [Required]
    public int ExternalOrderItemId { get; set; }

    /// <summary>
    /// Product/SKU code from external service
    /// </summary>
    [StringLength(50)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Name/description from external service
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity from external order
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure from external service
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit from external service
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Calculated total (Quantity × UnitPrice)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Currency code (inherited from PurchaseOrder)
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Expected delivery date for this item
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Item-specific notes from external service
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When this data was last retrieved from external service
    /// </summary>
    [Required]
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Flag indicating if external item has been modified since caching
    /// </summary>
    [Required]
    public bool ExternallyModified { get; set; } = false;

    /// <summary>
    /// Source service name (e.g., "OrderService", "QuotationService")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string SourceService { get; set; } = string.Empty;

    /// <summary>
    /// Version/ETag from external service for concurrency control
    /// </summary>
    [StringLength(100)]
    public string? ExternalVersion { get; set; }

    /// <summary>
    /// Status of the item in the external service
    /// </summary>
    [StringLength(50)]
    public string? ExternalStatus { get; set; }

    /// <summary>
    /// Last sync status with external service
    /// </summary>
    [Required]
    public bool IsSyncSuccessful { get; set; } = true;

    /// <summary>
    /// Last sync error message (if any)
    /// </summary>
    [StringLength(500)]
    public string? LastSyncError { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent purchase order
    /// </summary>
    [ForeignKey("PurchaseOrderId")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}