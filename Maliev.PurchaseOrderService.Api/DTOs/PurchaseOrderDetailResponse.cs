namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Detailed purchase order response including all related data
/// </summary>
public class PurchaseOrderDetailResponse : PurchaseOrderResponse
{
    /// <summary>
    /// Gets or sets the list of order items associated with the purchase order.
    /// </summary>
    public List<OrderItemResponse> Items { get; set; } = new();
    /// <summary>
    /// Gets or sets the shipping address for the purchase order.
    /// </summary>
    public AddressResponse? ShippingAddress { get; set; }
    /// <summary>
    /// Gets or sets the billing address for the purchase order.
    /// </summary>
    public AddressResponse? BillingAddress { get; set; }
    /// <summary>
    /// Gets or sets the list of files attached to the purchase order.
    /// </summary>
    public List<PurchaseOrderFileResponse>? Files { get; set; }
}