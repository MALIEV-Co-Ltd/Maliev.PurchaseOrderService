namespace Maliev.PurchaseOrderService.Data.Entities
{
    using System;
    using System.Collections.Generic;

    public partial class Address
    {
        public Address()
        {
            this.PurchaseOrderBillingAddress = new HashSet<PurchaseOrder>();
            this.PurchaseOrderShippingAddress = new HashSet<PurchaseOrder>();
        }

        public int Id { get; set; }
        public required string Building { get; set; }
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string State { get; set; }
        public required string PostalCode { get; set; }
        public int CountryId { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<PurchaseOrder> PurchaseOrderBillingAddress { get; set; }
        public virtual ICollection<PurchaseOrder> PurchaseOrderShippingAddress { get; set; }
    }
}
