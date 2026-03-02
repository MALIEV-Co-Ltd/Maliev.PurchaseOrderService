using Maliev.PurchaseOrderService.Domain.Enumerations;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Implementation of audit log service
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly PurchaseOrderContext _context;
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogService(PurchaseOrderContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task LogAuditAsync(
        string entityType,
        string entityId,
        AuditAction action,
        string userId,
        string userRole,
        string? oldValues = null,
        string? newValues = null,
        string? changeReason = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            UserRole = userRole,
            Timestamp = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
            ChangeReason = changeReason
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log created: {Action} on {EntityType} {EntityId} by {UserId}",
            action, entityType, entityId, userId);
    }
}
