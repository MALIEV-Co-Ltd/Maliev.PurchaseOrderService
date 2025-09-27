using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Represents a request to purchase goods or services from a supplier, linked to external orders and suppliers.
/// Aggregate root for the purchase order domain.
/// </summary>
[Table("PurchaseOrders")]
public class PurchaseOrder
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated unique internal order number (e.g., "PO-2025-001234")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional customer purchase order number for external POs
    /// </summary>
    [StringLength(50)]
    public string? CustomerPO { get; set; }

    /// <summary>
    /// References a supplier in SupplierService
    /// </summary>
    [Required]
    public int SupplierID { get; set; }

    /// <summary>
    /// References an order/quotation/internal order in OrderService
    /// </summary>
    [Required]
    public int OrderID { get; set; }

    /// <summary>
    /// References a currency in CurrencyService
    /// </summary>
    [Required]
    public int CurrencyID { get; set; }

    /// <summary>
    /// Cached supplier name from SupplierService
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Cached contact information
    /// </summary>
    [StringLength(200)]
    public string? SupplierContactInfo { get; set; }

    /// <summary>
    /// Cached currency code from CurrencyService (e.g., "THB", "USD")
    /// </summary>
    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Cached currency symbol for display
    /// </summary>
    [Required]
    [StringLength(10)]
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>
    /// Date when the purchase order was created
    /// </summary>
    [Required]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Order status (Pending, Approved, Ordered, Delivered, Cancelled)
    /// </summary>
    [Required]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Internal (company operations) or External (client projects)
    /// </summary>
    [Required]
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Subtotal amount before WHT calculated from derived order items
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Withholding tax rate percentage (0.00-99.99%)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Calculated withholding tax amount
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? WHTAmount { get; set; }

    /// <summary>
    /// Final total amount after WHT deduction (SubtotalAmount - WHTAmount)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "EUR") - duplicate for compatibility
    /// </summary>
    [Required]
    [StringLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the order
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who last modified the order
    /// </summary>
    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who approved the order
    /// </summary>
    [StringLength(50)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Approval timestamp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// User ID who cancelled the order
    /// </summary>
    [StringLength(50)]
    public string? CancelledBy { get; set; }

    /// <summary>
    /// Cancellation timestamp
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Optimistic concurrency control token
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Flag indicating if PDF generation is enabled for this purchase order
    /// </summary>
    [Required]
    public bool IsPdfGenerationEnabled { get; set; } = false;

    /// <summary>
    /// Flag indicating if PDF has been generated for this purchase order
    /// </summary>
    [Required]
    public bool IsPdfGenerated { get; set; } = false;

    /// <summary>
    /// Date and time when PDF was generated
    /// </summary>
    public DateTime? PdfGeneratedAt { get; set; }

    /// <summary>
    /// User who triggered PDF generation
    /// </summary>
    [StringLength(50)]
    public string? PdfGeneratedBy { get; set; }

    /// <summary>
    /// Reference to the generated PDF file in PurchaseOrderFiles
    /// </summary>
    public int? GeneratedPdfFileId { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// User ID who deleted the order
    /// </summary>
    [StringLength(50)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Deletion timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Individual line items derived from OrderService/QuotationService
    /// </summary>
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Foreign Key Properties for Addresses

    /// <summary>
    /// Foreign key for shipping address
    /// </summary>
    public int? ShippingAddressId { get; set; }

    /// <summary>
    /// Foreign key for billing address
    /// </summary>
    public int? BillingAddressId { get; set; }

    // Navigation Properties

    /// <summary>
    /// Shipping address
    /// </summary>
    [ForeignKey("ShippingAddressId")]
    public virtual Address? ShippingAddress { get; set; }

    /// <summary>
    /// Billing address
    /// </summary>
    [ForeignKey("BillingAddressId")]
    public virtual Address? BillingAddress { get; set; }

    /// <summary>
    /// Documents uploaded for this purchase order
    /// </summary>
    public virtual ICollection<PurchaseOrderFile> PurchaseOrderFiles { get; set; } = new List<PurchaseOrderFile>();

    /// <summary>
    /// Reference to the generated PDF file (if any)
    /// </summary>
    [ForeignKey("GeneratedPdfFileId")]
    public virtual PurchaseOrderFile? GeneratedPdfFile { get; set; }

    // Note: DomainEvents are queried by AggregateId to maintain loose coupling

    // Note: AuditLogs are queried by EntityId to maintain loose coupling
}