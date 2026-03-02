namespace Maliev.PurchaseOrderService.Application.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(Guid entityId, string entityType, string action, string details, string userId, CancellationToken cancellationToken = default);
}
