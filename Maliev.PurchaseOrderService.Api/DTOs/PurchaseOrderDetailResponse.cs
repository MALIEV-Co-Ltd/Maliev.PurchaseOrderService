namespace Maliev.PurchaseOrderService.Api.DTOs;

public class PurchaseOrderDetailResponse : PurchaseOrderResponse
{
    public List<OrderItemResponse> Items { get; set; } = new();
    public List<OrderItemResponse> OrderItems { get; set; } = new();
    public List<PurchaseOrderFileDto> PurchaseOrderFiles { get; set; } = new();
    public AddressResponse? ShippingAddress { get; set; }
    public AddressResponse? BillingAddress { get; set; }
}