namespace Maliev.PurchaseOrderService.Api.DTOs
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int? PurchaseOrderId { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Subtotal { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int? PurchaseOrderId { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class UpdateOrderItemDto
    {
        public int? PurchaseOrderId { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
