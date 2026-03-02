using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Domain.Entities;

public class PurchaseOrderFile
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
