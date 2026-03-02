using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Domain.Enumerations;

public enum DocumentType
{
    CustomerPO = 0,
    InternalApproval = 1,
    Invoice = 2,
    Reference = 3,
    GeneratedPDF = 4,
    Other = 5
}
