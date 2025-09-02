namespace Maliev.PurchaseOrderService.Data.Entities
{
    using System;
    using System.Collections.Generic;

    public partial class OrderItem
    {
        public int Id { get; set; }
        public int? PurchaseOrderId { get; set; }
        public string? PartNumber { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Subtotal { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual PurchaseOrder? PurchaseOrder { get; set; }
    }
}
