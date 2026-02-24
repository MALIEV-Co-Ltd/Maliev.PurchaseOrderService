namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Read-only line items derived from OrderService/QuotationService
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Unique identifier for the order item
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the purchase order
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Reference to the original item in OrderService
    /// </summary>
    public int ExternalOrderItemId { get; set; }

    /// <summary>
    /// Product code or SKU
    /// </summary>
    public string? ProductCode { get; set; }

    /// <summary>
    /// Name of the product
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure (e.g., "pcs", "kg")
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Unit price of the item
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Calculated total (Quantity × UnitPrice)
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "THB")
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Expected delivery date for this specific item
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Additional notes or comments for the item
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When this data was last retrieved from external service
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Flag indicating if external item has been modified since caching
    /// </summary>
    public bool ExternallyModified { get; set; }

    /// <summary>
    /// Navigation property to the parent PurchaseOrder
    /// </summary>
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
