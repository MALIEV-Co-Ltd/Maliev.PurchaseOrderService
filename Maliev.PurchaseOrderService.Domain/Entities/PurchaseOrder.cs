using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Domain.Entities;

public class PurchaseOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? CustomerPO { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierContactInfo { get; set; }
    public int OrderID { get; set; }
    public int CurrencyID { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; }
    public OrderType OrderType { get; set; }
    public int DepartmentId { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal? WHTRate { get; set; }
    public decimal? WHTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public int? ShippingAddressId { get; set; }
    public virtual Address? ShippingAddress { get; set; }
    public int? BillingAddressId { get; set; }
    public virtual Address? BillingAddress { get; set; }
    public virtual ICollection<PurchaseOrderFile> Files { get; set; } = new List<PurchaseOrderFile>();
}
