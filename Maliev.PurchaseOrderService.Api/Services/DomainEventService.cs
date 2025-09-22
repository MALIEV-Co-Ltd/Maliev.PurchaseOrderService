using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for domain event processing
/// </summary>
public class DomainEventService : IDomainEventService
{
    private readonly PurchaseOrderContext _context;
    private readonly IMapper _mapper;
    private readonly IPdfGenerationService _pdfService;
    private readonly ILogger<DomainEventService> _logger;

    // Event processing configuration
    private const int MaxRetryAttempts = 3;
    private readonly TimeSpan[] RetryDelays = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };

    public DomainEventService(
        PurchaseOrderContext context,
        IMapper mapper,
        IPdfGenerationService pdfService,
        ILogger<DomainEventService> logger)
    {
        _context = context;
        _mapper = mapper;
        _pdfService = pdfService;
        _logger = logger;
    }

    public async Task<long> PublishEventAsync(
        DomainEventDto domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing domain event {EventType} for {AggregateType} {AggregateId}",
            domainEvent.EventType, domainEvent.AggregateType, domainEvent.AggregateId);

        try
        {
            var entity = new DomainEvent
            {
                AggregateType = domainEvent.AggregateType,
                AggregateId = domainEvent.AggregateId,
                EventType = domainEvent.EventType,
                EventData = domainEvent.EventData ?? "",
                EventVersion = domainEvent.EventVersion,
                UserId = domainEvent.UserId ?? "",
                OccurredAt = domainEvent.OccurredAt,
                IsProcessed = false,
                ProcessedAt = null,
                ProcessingAttempts = 0,
                CorrelationId = domainEvent.CorrelationId ?? Guid.NewGuid().ToString()
            };

            _context.DomainEvents.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Domain event {EventId} published successfully", entity.Id);
            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing domain event {EventType} for {AggregateType} {AggregateId}",
                domainEvent.EventType, domainEvent.AggregateType, domainEvent.AggregateId);
            throw;
        }
    }

    public async Task<IEnumerable<DomainEventDto>> GetUnprocessedEventsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.DomainEvents
            .Where(de => !de.IsProcessed && de.ProcessingAttempts < MaxRetryAttempts)
            .OrderBy(de => de.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DomainEventDto>>(events);
    }

    public async Task<bool> MarkEventAsProcessedAsync(
        long eventId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Marking domain event {EventId} as processed", eventId);

        try
        {
            var domainEvent = await _context.DomainEvents
                .FirstOrDefaultAsync(de => de.Id == eventId, cancellationToken);

            if (domainEvent == null)
            {
                _logger.LogWarning("Domain event {EventId} not found", eventId);
                return false;
            }

            domainEvent.IsProcessed = true;
            domainEvent.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Domain event {EventId} marked as processed", eventId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking domain event {EventId} as processed", eventId);
            throw;
        }
    }

    public async Task<IEnumerable<DomainEventDto>> GetEventsByEntityAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var events = await _context.DomainEvents
            .Where(de => de.AggregateType == entityType && de.AggregateId == entityId)
            .OrderByDescending(de => de.OccurredAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DomainEventDto>>(events);
    }

    public async Task<int> ProcessUnprocessedEventsAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing unprocessed domain events (batch size: {BatchSize})", batchSize);

        var unprocessedEvents = await GetUnprocessedEventsAsync(batchSize, cancellationToken);
        var processedCount = 0;

        foreach (var eventDto in unprocessedEvents)
        {
            try
            {
                var processed = await ProcessSingleEventAsync(eventDto, cancellationToken);
                if (processed)
                {
                    await MarkEventAsProcessedAsync(eventDto.Id, cancellationToken);
                    processedCount++;
                }
                else
                {
                    await IncrementRetryCountAsync(eventDto.Id, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing domain event {EventId}", eventDto.Id);
                await IncrementRetryCountAsync(eventDto.Id, cancellationToken, ex.Message);
            }
        }

        _logger.LogInformation("Processed {ProcessedCount} domain events", processedCount);
        return processedCount;
    }


    public async Task<DomainEventStatsDto> GetEventStatsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting domain event statistics from {FromDate} to {ToDate}", fromDate, toDate);

        try
        {
            var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow;

            var query = _context.DomainEvents.Where(de => de.OccurredAt >= from && de.OccurredAt <= to);

            var stats = new DomainEventStatsDto
            {
                GeneratedAt = DateTime.UtcNow,
                FromDate = from,
                ToDate = to
            };

            // Basic counts
            stats.TotalEvents = await query.CountAsync(cancellationToken);
            stats.ProcessedEvents = await query.CountAsync(de => de.IsProcessed, cancellationToken);
            stats.UnprocessedEvents = await query.CountAsync(de => !de.IsProcessed, cancellationToken);
            stats.FailedEvents = await query.CountAsync(de => !de.IsProcessed && de.ProcessingAttempts >= MaxRetryAttempts, cancellationToken);

            // Average processing time (for processed events)
            var processedEventsWithTime = await query
                .Where(de => de.IsProcessed && de.ProcessedAt.HasValue)
                .Select(de => new { de.OccurredAt, de.ProcessedAt })
                .ToListAsync(cancellationToken);

            if (processedEventsWithTime.Any())
            {
                var processingTimes = processedEventsWithTime
                    .Select(e => (e.ProcessedAt!.Value - e.OccurredAt).TotalMilliseconds)
                    .ToList();

                stats.AverageProcessingTimeMs = processingTimes.Average();
            }

            // Events by type
            var eventsByType = await query
                .GroupBy(de => de.EventType)
                .Select(g => new { EventType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventType, x => x.Count, cancellationToken);

            stats.EventsByType = eventsByType;

            // Events by entity type
            var eventsByEntityType = await query
                .GroupBy(de => de.AggregateType)
                .Select(g => new { AggregateType = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AggregateType, x => x.Count, cancellationToken);

            stats.EventsByEntityType = eventsByEntityType;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting domain event statistics");
            throw;
        }
    }

    public async Task<int> CleanupOldEventsAsync(
        int retentionDays = 90,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up domain events older than {RetentionDays} days", retentionDays);

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldEvents = await _context.DomainEvents
                .Where(de => de.IsProcessed && de.OccurredAt < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldEvents.Any())
            {
                _context.DomainEvents.RemoveRange(oldEvents);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cleaned up {Count} old domain events", oldEvents.Count);
                return oldEvents.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old domain events");
            throw;
        }
    }

    private async Task<bool> ProcessSingleEventAsync(DomainEventDto eventDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing domain event {EventId}: {EventType} for {AggregateType} {AggregateId}",
            eventDto.Id, eventDto.EventType, eventDto.AggregateType, eventDto.AggregateId);

        try
        {
            switch (eventDto.EventType)
            {
                case "PurchaseOrderCreated":
                case "PurchaseOrderUpdated":
                    return await _pdfService.HandlePdfGenerationEventAsync(eventDto, cancellationToken);

                case "PurchaseOrderApproved":
                    return await HandlePurchaseOrderApprovedEventAsync(eventDto, cancellationToken);

                case "PurchaseOrderCanceled":
                    return await HandlePurchaseOrderCanceledEventAsync(eventDto, cancellationToken);

                default:
                    _logger.LogWarning("Unknown event type {EventType} for event {EventId}", eventDto.EventType, eventDto.Id);
                    return true; // Mark as processed to avoid infinite retries
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain event {EventId}", eventDto.Id);
            return false;
        }
    }

    private async Task<bool> HandlePurchaseOrderApprovedEventAsync(DomainEventDto eventDto, CancellationToken cancellationToken)
    {
        try
        {
            // Handle PDF generation for approved purchase orders
            if (eventDto.AggregateType == "PurchaseOrder" && int.TryParse(eventDto.AggregateId, out var purchaseOrderId))
            {
                var purchaseOrder = await _context.PurchaseOrders.FindAsync(new object[] { purchaseOrderId }, cancellationToken);
                if (purchaseOrder != null)
                {
                    var purchaseOrderDto = _mapper.Map<PurchaseOrderDto>(purchaseOrder);

                    // Generate PDF if applicable (internal POs only)
                    if (_pdfService.IsPdfGenerationApplicable(purchaseOrderDto))
                    {
                        var pdfResult = await _pdfService.GeneratePurchaseOrderPdfAsync(purchaseOrderId, cancellationToken);
                        if (!pdfResult.Success)
                        {
                            _logger.LogWarning("PDF generation failed for approved purchase order {PurchaseOrderId}: {Error}",
                                purchaseOrderId, pdfResult.ErrorMessage);
                            return false;
                        }
                    }

                    // Additional approval processing could go here
                    // e.g., sending notifications, updating external systems, etc.

                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling purchase order approved event {EventId}", eventDto.Id);
            return false;
        }
    }

    private async Task<bool> HandlePurchaseOrderCanceledEventAsync(DomainEventDto eventDto, CancellationToken cancellationToken)
    {
        try
        {
            // Handle purchase order cancellation
            if (eventDto.AggregateType == "PurchaseOrder" && int.TryParse(eventDto.AggregateId, out var purchaseOrderId))
            {
                // Archive related documents if specified
                var eventData = JsonSerializer.Deserialize<JsonElement>(eventDto.EventData ?? "{}");

                if (eventData.TryGetProperty("ArchiveDocuments", out var archiveProp) &&
                    archiveProp.GetBoolean())
                {
                    var files = await _context.PurchaseOrderFiles
                        .Where(f => f.PurchaseOrderId == purchaseOrderId && !f.IsDeleted)
                        .ToListAsync(cancellationToken);

                    foreach (var file in files)
                    {
                        // IsArchived and UpdatedAt properties not available in PurchaseOrderFile entity
                        // Consider implementing archiving through different approach
                    }

                    if (files.Any())
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                // Additional cancellation processing could go here
                // e.g., sending notifications, updating external systems, etc.

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling purchase order canceled event {EventId}", eventDto.Id);
            return false;
        }
    }

    private async Task IncrementRetryCountAsync(long eventId, CancellationToken cancellationToken, string? errorMessage = null)
    {
        try
        {
            var domainEvent = await _context.DomainEvents
                .FirstOrDefaultAsync(de => de.Id == eventId, cancellationToken);

            if (domainEvent != null)
            {
                domainEvent.ProcessingAttempts++;
                domainEvent.LastProcessingError = errorMessage;

                // Set next retry time if within retry limits
                if (domainEvent.ProcessingAttempts < MaxRetryAttempts)
                {
                    var retryDelay = RetryDelays[Math.Min(domainEvent.ProcessingAttempts - 1, RetryDelays.Length - 1)];
                    // NextRetryAt property not available in entity - implement retry logic elsewhere
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing retry count for domain event {EventId}", eventId);
        }
    }

    public async Task<DomainEventDto?> GetEventByIdAsync(long eventId, CancellationToken cancellationToken = default)
    {
        var domainEvent = await _context.DomainEvents
            .FirstOrDefaultAsync(de => de.Id == eventId, cancellationToken);

        return domainEvent != null ? _mapper.Map<DomainEventDto>(domainEvent) : null;
    }

    public async Task<bool> RetryEventAsync(long eventId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrying domain event {EventId}", eventId);

        try
        {
            var domainEvent = await _context.DomainEvents
                .FirstOrDefaultAsync(de => de.Id == eventId, cancellationToken);

            if (domainEvent == null)
            {
                _logger.LogWarning("Domain event {EventId} not found for retry", eventId);
                return false;
            }

            if (domainEvent.IsProcessed)
            {
                _logger.LogWarning("Domain event {EventId} is already processed", eventId);
                return false;
            }

            if (domainEvent.ProcessingAttempts >= MaxRetryAttempts)
            {
                _logger.LogWarning("Domain event {EventId} has exceeded maximum retry attempts", eventId);
                return false;
            }

            var eventDto = _mapper.Map<DomainEventDto>(domainEvent);
            var processed = await ProcessSingleEventAsync(eventDto, cancellationToken);

            if (processed)
            {
                await MarkEventAsProcessedAsync(eventId, cancellationToken);
                _logger.LogInformation("Domain event {EventId} retry successful", eventId);
                return true;
            }
            else
            {
                await IncrementRetryCountAsync(eventId, cancellationToken);
                _logger.LogWarning("Domain event {EventId} retry failed", eventId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying domain event {EventId}", eventId);
            await IncrementRetryCountAsync(eventId, cancellationToken, ex.Message);
            return false;
        }
    }
}