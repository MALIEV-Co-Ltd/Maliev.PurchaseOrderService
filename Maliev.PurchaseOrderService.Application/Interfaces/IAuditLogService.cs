using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAuditAsync(
        string entityType,
        string entityId,
        AuditAction action,
        string userId,
        string userRole,
        string? oldValues = null,
        string? newValues = null,
        string? changeReason = null,
        CancellationToken cancellationToken = default);
}
