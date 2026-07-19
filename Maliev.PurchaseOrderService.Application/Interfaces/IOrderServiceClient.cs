namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IOrderServiceClient
{
    Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);
    Task<List<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<List<OrderItemDto>> GetOrderItemsAsync(string orderId, CancellationToken cancellationToken = default);
    Task<bool> ValidateOrderExistsAsync(int orderId, CancellationToken cancellationToken = default);
}

public class OrderDto
{
    public int Id { get; set; }
    public string SourceOrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
