using System.Globalization;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;
using Maliev.PurchaseOrderService.Domain.Constants;
using Maliev.PurchaseOrderService.Domain.Entities;

namespace Maliev.PurchaseOrderService.Infrastructure.Search;

/// <summary>
/// Maps purchase order records to centralized global search documents.
/// </summary>
public static class PurchaseOrderSearchDocumentMapper
{
    private const string SourceService = "PurchaseOrderService";
    private const string ResourceType = "purchase-order";

    /// <summary>
    /// Creates a search upsert event for a purchase order.
    /// </summary>
    /// <param name="purchaseOrder">Purchase order to index.</param>
    /// <param name="occurredAtUtc">Timestamp for the source change.</param>
    /// <returns>A centralized search upsert event.</returns>
    public static SearchDocumentUpsertedEvent ToUpsertEvent(PurchaseOrder purchaseOrder, DateTimeOffset occurredAtUtc)
    {
        var title = string.IsNullOrWhiteSpace(purchaseOrder.OrderNumber)
            ? $"Purchase order {purchaseOrder.Id.ToString(CultureInfo.InvariantCulture)}"
            : purchaseOrder.OrderNumber;

        var subtitle = string.IsNullOrWhiteSpace(purchaseOrder.SupplierName)
            ? null
            : purchaseOrder.SupplierName;

        var summary = string.Join(" ",
            purchaseOrder.OrderType.ToString(),
            purchaseOrder.CurrencyCode,
            purchaseOrder.TotalAmount.ToString("N2", CultureInfo.InvariantCulture),
            purchaseOrder.ExpectedDeliveryDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty)
            .Trim();

        var keywords = CompactKeywords(
            purchaseOrder.Id.ToString(CultureInfo.InvariantCulture),
            purchaseOrder.OrderNumber,
            purchaseOrder.SupplierID.ToString(CultureInfo.InvariantCulture),
            purchaseOrder.SupplierName,
            purchaseOrder.OrderID.ToString(CultureInfo.InvariantCulture),
            purchaseOrder.CustomerPO,
            purchaseOrder.CurrencyCode,
            purchaseOrder.Status.ToString(),
            purchaseOrder.OrderType.ToString());

        return new SearchDocumentUpsertedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: nameof(SearchDocumentUpsertedEvent),
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: SourceService,
            ConsumedBy: ["SearchService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: occurredAtUtc,
            IsPublic: false,
            Payload: new SearchDocumentUpsertedEventPayload(
                SourceService: SourceService,
                ResourceType: ResourceType,
                ResourceId: purchaseOrder.Id.ToString(CultureInfo.InvariantCulture),
                Title: title,
                Subtitle: subtitle,
                Summary: string.IsNullOrWhiteSpace(summary) ? null : summary,
                Keywords: keywords,
                Status: purchaseOrder.Status.ToString(),
                RequiredPermission: PurchaseOrderPermissions.Orders.Read,
                OccurredAtUtc: occurredAtUtc));
    }

    private static IReadOnlyList<string> CompactKeywords(params string?[] values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
