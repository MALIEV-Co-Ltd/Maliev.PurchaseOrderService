using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Domain.Enumerations;

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Approve = 3,
    Cancel = 4,
    ExternalFetch = 5,
    ExternalValidation = 6,
    PDFGenerated = 7,
    EventPublished = 8
}
