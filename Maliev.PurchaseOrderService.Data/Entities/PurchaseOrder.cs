using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Represents a purchase order (internal or external) aggregate root
/// </summary>
public class PurchaseOrder
{
    /// <summary>
    /// Unique identifier for the purchase order
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated unique internal order number (e.g., "PO-2025-001234")
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional customer purchase order number for external POs
    /// </summary>
    public string? CustomerPO { get; set; }

    /// <summary>
    /// References a supplier in SupplierService
    /// </summary>
    public int SupplierID { get; set; }

    /// <summary>
    /// Cached supplier name from SupplierService
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Cached contact information
    /// </summary>
    public string? SupplierContactInfo { get; set; }

    /// <summary>
    /// References an order/quotation in OrderService
    /// </summary>
    public int OrderID { get; set; }

    /// <summary>
    /// References a currency in CurrencyService
    /// </summary>
    public int CurrencyID { get; set; }

    /// <summary>
    /// Cached currency code from CurrencyService (e.g., "THB", "USD")
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Cached currency symbol for display
    /// </summary>
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>
    /// Date the order was created/issued
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Expected delivery date for the order
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Current status of the order
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Type of the order (Internal or External).
    /// </summary>
    public OrderType OrderType { get; set; }

    /// <summary>
    /// ID of the department that owns this purchase order.
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// Subtotal amount before WHT calculated from derived order items
    /// </summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Withholding tax rate percentage (0.00-99.99%)
    /// </summary>
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Calculated withholding tax amount
    /// </summary>
    public decimal? WHTAmount { get; set; }

    /// <summary>
    /// Final total amount after WHT deduction (SubtotalAmount - WHTAmount)
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// User ID who created the order
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the order was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who last modified the order
    /// </summary>
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Date and time when the order was last modified
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// User ID who approved the order
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Date and time when the order was approved
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Optimistic concurrency control token (mapped to PostgreSQL xmin)
    /// </summary>
    public uint RowVersion { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// User ID who deleted the order
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Date and time when the order was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Navigation property for order items
    /// </summary>
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    /// <summary>
    /// Foreign key for shipping address
    /// </summary>
    public int? ShippingAddressId { get; set; }

    /// <summary>
    /// Navigation property for shipping address
    /// </summary>
    public virtual Address? ShippingAddress { get; set; }

    /// <summary>
    /// Foreign key for billing address
    /// </summary>
    public int? BillingAddressId { get; set; }

    /// <summary>
    /// Navigation property for billing address
    /// </summary>
    public virtual Address? BillingAddress { get; set; }

    /// <summary>
    /// Navigation property for attached files
    /// </summary>
    public virtual ICollection<PurchaseOrderFile> Files { get; set; } = new List<PurchaseOrderFile>();
}
