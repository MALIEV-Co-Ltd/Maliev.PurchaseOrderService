using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ExternalServiceName { get; set; }
    public string? ExternalServiceResponse { get; set; }
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? ChangeReason { get; set; }
}
