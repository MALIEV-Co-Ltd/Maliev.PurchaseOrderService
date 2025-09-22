namespace Maliev.PurchaseOrderService.Data.Enums;

/// <summary>
/// Status of a purchase order through its lifecycle
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Initial status when order is created
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Order has been approved by manager/procurement
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Order has been sent to supplier
    /// </summary>
    Ordered = 2,

    /// <summary>
    /// Order has been received
    /// </summary>
    Delivered = 3,

    /// <summary>
    /// Order has been cancelled
    /// </summary>
    Cancelled = 4
}