namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for creating a new order
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// Customer ID for the order
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// List of order items
    /// </summary>
    public List<CreateOrderItemRequest> Items { get; set; } = new();

    /// <summary>
    /// Delivery address for the order
    /// </summary>
    public string DeliveryAddress { get; set; } = string.Empty;

    /// <summary>
    /// Additional notes for the order
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Order priority level
    /// </summary>
    public string Priority { get; set; } = "Normal";
}

