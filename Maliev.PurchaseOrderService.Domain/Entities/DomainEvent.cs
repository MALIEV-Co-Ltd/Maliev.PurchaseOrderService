using Maliev.PurchaseOrderService.Domain.Entities;
namespace Maliev.PurchaseOrderService.Domain.Entities;

public class DomainEvent
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public int EventVersion { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
    public int ProcessingAttempts { get; set; }
    public string? LastProcessingError { get; set; }
}
