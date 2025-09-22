using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Stores domain events for event-driven architecture, particularly for triggering PDF generation and other async processes
/// </summary>
[Table("DomainEvents")]
public class DomainEvent
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Type of event (e.g., "PurchaseOrderCreated", "PurchaseOrderUpdated")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the aggregate that generated the event
    /// </summary>
    [Required]
    [StringLength(50)]
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Type of aggregate (e.g., "PurchaseOrder")
    /// </summary>
    [Required]
    [StringLength(50)]
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data - JSON format
    /// </summary>
    [Required]
    [Column(TypeName = "text")]
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Version of the event for backward compatibility
    /// </summary>
    [Required]
    public int EventVersion { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    [Required]
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [Required]
    [StringLength(100)]
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// User who triggered the event
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the event has been processed
    /// </summary>
    [Required]
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Number of processing attempts (for retry logic)
    /// </summary>
    [Required]
    public int ProcessingAttempts { get; set; } = 0;

    /// <summary>
    /// Last processing error message
    /// </summary>
    [StringLength(1000)]
    public string? LastProcessingError { get; set; }

    /// <summary>
    /// Next retry attempt time (for failed events)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    [Required]
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Priority of the event for processing order
    /// </summary>
    [Required]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Source of the event (e.g., "PurchaseOrderService", "System")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventSource { get; set; } = string.Empty;

    /// <summary>
    /// Headers/metadata for event processing
    /// </summary>
    [Column(TypeName = "text")]
    public string? EventHeaders { get; set; }

    /// <summary>
    /// Partition key for event ordering (optional)
    /// </summary>
    [StringLength(100)]
    public string? PartitionKey { get; set; }

    /// <summary>
    /// Expiration time for the event
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Handler that processed this event
    /// </summary>
    [StringLength(100)]
    public string? ProcessedByHandler { get; set; }

    /// <summary>
    /// Result of event processing
    /// </summary>
    [Column(TypeName = "text")]
    public string? ProcessingResult { get; set; }
}