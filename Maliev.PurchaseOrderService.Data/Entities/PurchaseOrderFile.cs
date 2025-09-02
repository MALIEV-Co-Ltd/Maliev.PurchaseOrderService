namespace Maliev.PurchaseOrderService.Data.Entities
{
    using System;
    using System.Collections.Generic;

    public partial class PurchaseOrderFile
    {
        public int Id { get; set; }
        public int? PurchaseOrderId { get; set; }
        public required string Bucket { get; set; }
        public required string ObjectName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual PurchaseOrder? PurchaseOrder { get; set; }
    }
}
