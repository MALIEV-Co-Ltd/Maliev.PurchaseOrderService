using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Tracks all changes made to purchase orders and external service interactions
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Entity type (e.g., "PurchaseOrder", "OrderItem", "ExternalServiceCall")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Action performed on the entity
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// ID of the user who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Role of the user who performed the action
    /// </summary>
    public string UserRole { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Previous values (for updates) in JSON format
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (for creates/updates) in JSON format
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Name of external service called
    /// </summary>
    public string? ExternalServiceName { get; set; }

    /// <summary>
    /// Response from external service (sensitive data masked) in JSON format
    /// </summary>
    public string? ExternalServiceResponse { get; set; }

    /// <summary>
    /// IP address of the client
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// User agent string of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Reason for the change (if provided)
    /// </summary>
    public string? ChangeReason { get; set; }
}