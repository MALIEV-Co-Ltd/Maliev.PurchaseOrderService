namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Domain event data transfer object
/// </summary>
public class DomainEventDto
{
    /// <summary>
    /// Event ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Type of aggregate that triggered the event
    /// </summary>
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the aggregate that triggered the event
    /// </summary>
    public string AggregateId { get; set; } = string.Empty;

    /// <summary>
    /// Type of event
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Event data as JSON
    /// </summary>
    public string? EventData { get; set; }

    /// <summary>
    /// Version of the event for backward compatibility
    /// </summary>
    public int EventVersion { get; set; } = 1;

    /// <summary>
    /// User who triggered the event
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Whether the event has been processed
    /// </summary>
    public bool IsProcessed { get; set; }

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int ProcessingAttempts { get; set; }

    /// <summary>
    /// Next retry timestamp
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Last processing error message
    /// </summary>
    public string? LastProcessingError { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    public string? CorrelationId { get; set; }
}