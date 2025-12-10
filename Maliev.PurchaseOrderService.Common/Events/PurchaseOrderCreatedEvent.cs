namespace Maliev.PurchaseOrderService.Common.Events;

/// <summary>
/// Event published when a purchase order is created
/// </summary>
public class PurchaseOrderCreatedEvent
{
    /// <summary>
    /// The unique identifier of the purchase order
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// The order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// The date when the purchase order was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
