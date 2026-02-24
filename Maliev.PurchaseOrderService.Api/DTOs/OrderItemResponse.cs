namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Order item response (read-only, derived from OrderService)
/// </summary>
public class OrderItemResponse
{
    /// <summary>
    /// Unique identifier for the order item.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Identifier of the external order item.
    /// </summary>
    public int ExternalOrderItemId { get; set; }
    /// <summary>
    /// Product code.
    /// </summary>
    public string? ProductCode { get; set; }
    /// <summary>
    /// Name of the product.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    /// <summary>
    /// Quantity of the product.
    /// </summary>
    public decimal Quantity { get; set; }
    /// <summary>
    /// Unit of measure for the product.
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;
    /// <summary>
    /// Unit price of the product.
    /// </summary>
    public decimal UnitPrice { get; set; }
    /// <summary>
    /// Total price for the order item.
    /// </summary>
    public decimal TotalPrice { get; set; }
    /// <summary>
    /// Currency of the price.
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    /// <summary>
    /// Additional notes for the order item.
    /// </summary>
    public string? Notes { get; set; }
    /// <summary>
    /// Timestamp when the item data was last cached.
    /// </summary>
    public DateTime CachedAt { get; set; }
    /// <summary>
    /// Indicates if the external item has been modified since caching.
    /// </summary>
    public bool ExternallyModified { get; set; }
}
