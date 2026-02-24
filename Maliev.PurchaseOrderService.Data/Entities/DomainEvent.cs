namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Stores domain events for event-driven architecture (e.g., triggering PDF generation)
/// </summary>
public class DomainEvent
{
    /// <summary>
    /// Unique identifier for the domain event
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Type of event (e.g., "PurchaseOrderCreated", "PurchaseOrderUpdated")
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the aggregate that generated the event
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Type of aggregate (e.g., "PurchaseOrder")
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Serialized event data in JSON format
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Version of the event for backward compatibility
    /// </summary>
    public int EventVersion { get; set; }

    /// <summary>
    /// Date and time when the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Date and time when the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user who triggered the event
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating if the event has been processed
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Number of processing attempts (for retry logic)
    /// </summary>
    public int ProcessingAttempts { get; set; }

    /// <summary>
    /// Error message from the last processing attempt (if any)
    /// </summary>
    public string? LastProcessingError { get; set; }
}
