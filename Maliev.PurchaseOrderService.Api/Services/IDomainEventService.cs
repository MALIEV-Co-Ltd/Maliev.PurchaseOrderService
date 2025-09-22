using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Interface for domain event processing service
/// </summary>
public interface IDomainEventService
{
    /// <summary>
    /// Publishes a domain event
    /// </summary>
    /// <param name="domainEvent">Domain event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event ID</returns>
    Task<long> PublishEventAsync(
        DomainEventDto domainEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all unprocessed domain events
    /// </summary>
    /// <param name="batchSize">Maximum number of events to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unprocessed events</returns>
    Task<IEnumerable<DomainEventDto>> GetUnprocessedEventsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a domain event as processed
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully marked as processed</returns>
    Task<bool> MarkEventAsProcessedAsync(
        long eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets domain events by entity
    /// </summary>
    /// <param name="entityType">Entity type</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events for the entity</returns>
    Task<IEnumerable<DomainEventDto>> GetEventsByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes all unprocessed events
    /// </summary>
    /// <param name="batchSize">Maximum number of events to process in one batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events processed</returns>
    Task<int> ProcessUnprocessedEventsAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets domain event by ID
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Domain event or null if not found</returns>
    Task<DomainEventDto?> GetEventByIdAsync(
        long eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed domain event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if retry was successful</returns>
    Task<bool> RetryEventAsync(
        long eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets domain event statistics
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Event processing statistics</returns>
    Task<DomainEventStatsDto> GetEventStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old processed events (for housekeeping)
    /// </summary>
    /// <param name="retentionDays">Number of days to retain events</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events cleaned up</returns>
    Task<int> CleanupOldEventsAsync(
        int retentionDays = 90,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Domain event processing statistics
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
    /// Number of failed events
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Average processing time in milliseconds
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Events by type breakdown
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// Events by entity type breakdown
    /// </summary>
    public Dictionary<string, int> EventsByEntityType { get; set; } = new();

    /// <summary>
    /// Statistics generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Date range for statistics
    /// </summary>
    public DateTime FromDate { get; set; }

    /// <summary>
    /// Date range for statistics
    /// </summary>
    public DateTime ToDate { get; set; }
}