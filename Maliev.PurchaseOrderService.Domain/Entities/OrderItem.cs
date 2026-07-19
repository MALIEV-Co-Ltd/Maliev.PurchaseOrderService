using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ExternalOrderItemId { get; set; }
    public string? SourceOrderItemId { get; set; }
    public string? ProductCode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CachedAt { get; set; }
    public bool ExternallyModified { get; set; }
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
