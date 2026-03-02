using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for managing audit logs
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Logs an audit event asynchronously.
    /// </summary>
    /// <param name="entityType">The type of the entity being audited (e.g., "PurchaseOrder").</param>
    /// <param name="entityId">The ID of the entity being audited.</param>
    /// <param name="action">The audit action performed.</param>
    /// <param name="userId">The ID of the user who performed the action.</param>
    /// <param name="userRole">The role of the user who performed the action.</param>
    /// <param name="oldValues">Optional: Old values of the entity in JSON format.</param>
    /// <param name="newValues">Optional: New values of the entity in JSON format.</param>
    /// <param name="changeReason">Optional: Reason for the change.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
