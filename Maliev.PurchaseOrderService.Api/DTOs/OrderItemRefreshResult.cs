namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of order items refresh operation
/// </summary>
public class OrderItemRefreshResult
{
    /// <summary>
    /// Purchase order ID
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// External order ID
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Whether the refresh was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if refresh failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of items before refresh
    /// </summary>
    public int PreviousItemCount { get; set; }

    /// <summary>
    /// Number of items after refresh
    /// </summary>
    public int NewItemCount { get; set; }

    /// <summary>
    /// Number of items added during refresh
    /// </summary>
    public int ItemsAdded { get; set; }

    /// <summary>
    /// Number of items removed during refresh
    /// </summary>
    public int ItemsRemoved { get; set; }

    /// <summary>
    /// Whether the purchase order subtotal was updated
    /// </summary>
    public bool SubtotalUpdated { get; set; }

    /// <summary>
    /// New subtotal amount after refresh
    /// </summary>
    public decimal? NewSubtotal { get; set; }

    /// <summary>
    /// Timestamp when refresh was performed
    /// </summary>
    public DateTime RefreshedAt { get; set; }

    /// <summary>
    /// User who performed the refresh
    /// </summary>
    public string RefreshedBy { get; set; } = string.Empty;

    /// <summary>
    /// Number of order items successfully refreshed
    /// </summary>
    public int RefreshedCount { get; set; }

    /// <summary>
    /// Refreshed order items
    /// </summary>
    public List<OrderItemDto> OrderItems { get; set; } = new();
}