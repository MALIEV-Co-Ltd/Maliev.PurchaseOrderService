namespace Maliev.PurchaseOrderService.Data.Entities
{
    using System;
    using System.Collections.Generic;

    public partial class PurchaseOrder
    {
        public PurchaseOrder()
        {
            this.OrderItem = new HashSet<OrderItem>();
            this.PurchaseOrderFile = new HashSet<PurchaseOrderFile>();
        }

        public int Id { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierContactPerson { get; set; }
        public int? ShippingAddressId { get; set; }
        public string? ShippingContactPerson { get; set; }
        public string? ShippingTelephone { get; set; }
        public string? ShippingMobile { get; set; }
        public string? ShippingFax { get; set; }
        public int? BillingAddressId { get; set; }
        public string? BillingContactPerson { get; set; }
        public string? BillingTelephone { get; set; }
        public string? BillingMobile { get; set; }
        public string? BillingFax { get; set; }
        public string? Fob { get; set; }
        public string? Terms { get; set; }
        public string? ShippingMethod { get; set; }
        public int? EmployeeId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual Address? BillingAddress { get; set; }
        public virtual Address? ShippingAddress { get; set; }
        public virtual ICollection<OrderItem> OrderItem { get; set; }
        public virtual ICollection<PurchaseOrderFile> PurchaseOrderFile { get; set; }
    }
}
