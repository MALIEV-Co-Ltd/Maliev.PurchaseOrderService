namespace Maliev.PurchaseOrderService.Api.DTOs
{
    public class PurchaseOrderFileDto
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public required string Bucket { get; set; }
        public required string ObjectName { get; set; }
    }

    public class CreatePurchaseOrderFileDto
    {
        public int PurchaseOrderId { get; set; }
        public required string Bucket { get; set; }
        public required string ObjectName { get; set; }
    }

    public class UpdatePurchaseOrderFileDto
    {
        public int PurchaseOrderId { get; set; }
        public required string Bucket { get; set; }
        public required string ObjectName { get; set; }
    }
}
