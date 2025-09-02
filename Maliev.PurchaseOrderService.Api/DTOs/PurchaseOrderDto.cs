namespace Maliev.PurchaseOrderService.Api.DTOs
{
    using System;
    using System.Collections.Generic;

    public class PurchaseOrderDto
    {
        public PurchaseOrderDto()
        {
            OrderItem = new List<OrderItemDto>();
            PurchaseOrderFile = new List<PurchaseOrderFileDto>();
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

        public AddressDto? BillingAddress { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public ICollection<OrderItemDto> OrderItem { get; set; }
        public ICollection<PurchaseOrderFileDto> PurchaseOrderFile { get; set; }
    }

    public class CreatePurchaseOrderDto
    {
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
    }

    public class UpdatePurchaseOrderDto
    {
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
    }
}
