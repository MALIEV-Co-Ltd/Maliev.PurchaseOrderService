namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Domain event statistics
/// </summary>
public class DomainEventStatsDto
{
    /// <summary>
    /// Total number of events
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of processed events
    /// </summary>
    public int ProcessedEvents { get; set; }

    /// <summary>
    /// Number of unprocessed events
    /// </summary>
    public int UnprocessedEvents { get; set; }

    /// <summary>
    /// Number of failed events (exceeded max retries)
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Average processing time in milliseconds
    /// </summary>
    public double? AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Events grouped by type
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// Events grouped by entity type
    /// </summary>
    public Dictionary<string, int> EventsByEntityType { get; set; } = new();

    /// <summary>
    /// Start date of the statistics period
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// End date of the statistics period
    /// </summary>
    public DateTime ToDate { get; set; }

    /// <summary>
    /// Statistics generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }
}