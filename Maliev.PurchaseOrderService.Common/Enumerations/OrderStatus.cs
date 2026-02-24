namespace Maliev.PurchaseOrderService.Common.Enumerations;

/// <summary>
/// Represents the status of a purchase order throughout its lifecycle
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
    Cancelled = 4,

    /// <summary>
    /// PDF generation is pending (failed initial generation, awaiting retry)
    /// </summary>
    PDFPending = 5
}
