using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Domain.Enumerations;

public enum OrderStatus
{
    Pending = 0,
    Approved = 1,
    Ordered = 2,
    Delivered = 3,
    Cancelled = 4,
    PDFPending = 5
}
