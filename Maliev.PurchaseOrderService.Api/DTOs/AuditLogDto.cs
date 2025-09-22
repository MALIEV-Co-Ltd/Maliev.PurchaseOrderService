using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Audit log data transfer object
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// Audit log entry ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Type of entity being audited
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being audited
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Action performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Original data before change (for updates)
    /// </summary>
    public string? OriginalData { get; set; }

    /// <summary>
    /// New data after change (for updates)
    /// </summary>
    public string? NewData { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent information
    /// </summary>
    public string? UserAgent { get; set; }
}