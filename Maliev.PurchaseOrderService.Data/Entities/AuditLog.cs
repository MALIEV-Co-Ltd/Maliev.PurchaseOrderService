using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Tracks all changes made to purchase orders and external service interactions for compliance and audit trail
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Entity type (e.g., "PurchaseOrder", "OrderItem", "ExternalServiceCall")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the affected entity
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Type of action (Create, Update, Delete, Approve, Cancel, ExternalFetch, ExternalValidation, PDFGenerated, EventPublished)
    /// </summary>
    [Required]
    public AuditAction Action { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's role at time of action
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UserRole { get; set; } = string.Empty;

    /// <summary>
    /// When the action occurred
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Previous values (for updates) - JSON format
    /// </summary>
    [Column(TypeName = "text")]
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (for creates/updates) - JSON format
    /// </summary>
    [Column(TypeName = "text")]
    public string? NewValues { get; set; }

    /// <summary>
    /// Name of external service called
    /// </summary>
    [StringLength(50)]
    public string? ExternalServiceName { get; set; }

    /// <summary>
    /// Response from external service (sensitive data masked) - JSON format
    /// </summary>
    [Column(TypeName = "text")]
    public string? ExternalServiceResponse { get; set; }

    /// <summary>
    /// User's IP address
    /// </summary>
    [StringLength(45)] // IPv6 addresses can be up to 45 characters
    public string? IPAddress { get; set; }

    /// <summary>
    /// Browser/client information
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Reason for the change
    /// </summary>
    [StringLength(200)]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Request ID for HTTP request tracking
    /// </summary>
    [StringLength(100)]
    public string? RequestId { get; set; }

    /// <summary>
    /// Duration of the operation in milliseconds
    /// </summary>
    public long? OperationDurationMs { get; set; }

    /// <summary>
    /// Success status of the operation
    /// </summary>
    [Required]
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Application version when action occurred
    /// </summary>
    [StringLength(50)]
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Environment where action occurred (Development, Staging, Production)
    /// </summary>
    [StringLength(20)]
    public string? Environment { get; set; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    [Column(TypeName = "text")]
    public string? Metadata { get; set; }
}