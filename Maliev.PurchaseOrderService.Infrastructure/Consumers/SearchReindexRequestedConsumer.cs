using Maliev.MessagingContracts.Contracts.Search;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Infrastructure.Search;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maliev.PurchaseOrderService.Infrastructure.Consumers;

/// <summary>
/// Republishes purchase order search documents when SearchService requests a reindex.
/// </summary>
public class SearchReindexRequestedConsumer : IConsumer<SearchReindexRequestedCommand>
{
    private const string SourceService = "PurchaseOrderService";
    private readonly PurchaseOrderContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SearchReindexRequestedConsumer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchReindexRequestedConsumer"/> class.
    /// </summary>
    /// <param name="context">Purchase order database context.</param>
    /// <param name="publishEndpoint">MassTransit publish endpoint.</param>
    /// <param name="logger">Logger instance.</param>
    public SearchReindexRequestedConsumer(
        PurchaseOrderContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<SearchReindexRequestedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SearchReindexRequestedCommand> context)
    {
        if (!ShouldHandle(context.Message.Payload.SourceService))
        {
            return;
        }

        var count = 0;
        var occurredAtUtc = DateTimeOffset.UtcNow;

        await foreach (var purchaseOrder in _context.PurchaseOrders
            .AsNoTracking()
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken))
        {
            await _publishEndpoint.Publish(
                PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, occurredAtUtc),
                context.CancellationToken);
            count++;
        }

        _logger.LogInformation("Republished {Count} purchase order search documents", count);
    }

    private static bool ShouldHandle(string? sourceService)
    {
        return string.IsNullOrWhiteSpace(sourceService) ||
            string.Equals(sourceService, SourceService, StringComparison.OrdinalIgnoreCase);
    }
}
