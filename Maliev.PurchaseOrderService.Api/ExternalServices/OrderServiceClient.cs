using Maliev.PurchaseOrderService.Application.Interfaces;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Implementation of OrderService client
/// </summary>
public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderServiceClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public OrderServiceClient(IHttpClientFactory httpClientFactory, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OrderService");
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await GetOrderAsync(orderId.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OrderDto?> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(Uri.EscapeDataString(orderId), cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch order {OrderId}: {StatusCode}", orderId, response.StatusCode);
                return null;
            }

            var order = await response.Content.ReadFromJsonAsync<OrderServiceOrderResponse>(cancellationToken);
            return order?.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to fetch order {orderId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{orderId.ToString(System.Globalization.CultureInfo.InvariantCulture)}/items", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch order items for {OrderId}: {StatusCode}", orderId, response.StatusCode);
                return new List<OrderItemDto>();
            }

            return await response.Content.ReadFromJsonAsync<List<OrderItemDto>>(cancellationToken) ?? new List<OrderItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order items for {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to fetch order items for {orderId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<OrderItemDto>> GetOrderItemsAsync(string orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var order = await GetOrderAsync(orderId, cancellationToken);
            return order?.Items ?? new List<OrderItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order items for {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to fetch order items for {orderId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateOrderExistsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Base URL already includes /orders/v1/, so just request {id}
            var response = await _httpClient.GetAsync($"{orderId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order {OrderId}", orderId);
            return false;
        }
    }

    private sealed record OrderServiceOrderResponse(
        int? Id,
        string? SourceOrderId,
        string? OrderId,
        string? OrderNumber,
        DateTime? OrderDate,
        string? Status,
        List<OrderItemDto>? Items,
        string? ServiceCategoryName,
        string? ProcessTypeName,
        string? CurrentStatus,
        string? Requirements,
        int? OrderedQuantity,
        string? MaterialName,
        decimal? QuotedAmount,
        string? QuoteCurrency,
        DateTime CreatedAt)
    {
        public OrderDto ToDto()
        {
            var sourceOrderId = FirstNonEmpty(
                SourceOrderId,
                OrderId,
                Id?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                OrderNumber);
            var orderNumber = FirstNonEmpty(OrderNumber, sourceOrderId);

            if (Items is { Count: > 0 })
            {
                foreach (var item in Items.Where(item => string.IsNullOrWhiteSpace(item.SourceItemId)))
                {
                    item.SourceItemId = item.Id > 0
                        ? item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : "primary";
                }

                return new OrderDto
                {
                    Id = Id.GetValueOrDefault(),
                    SourceOrderId = sourceOrderId,
                    OrderNumber = orderNumber,
                    OrderDate = OrderDate ?? CreatedAt,
                    Status = Status ?? CurrentStatus ?? string.Empty,
                    Items = Items
                };
            }

            var quantity = OrderedQuantity.GetValueOrDefault(1);
            if (quantity <= 0)
            {
                quantity = 1;
            }

            var unitPrice = QuotedAmount.GetValueOrDefault() / quantity;
            var productName = FirstNonEmpty(ProcessTypeName, ServiceCategoryName, MaterialName, Requirements, $"Order {sourceOrderId}");

            return new OrderDto
            {
                Id = Id.GetValueOrDefault(),
                SourceOrderId = sourceOrderId,
                OrderNumber = orderNumber,
                OrderDate = OrderDate ?? CreatedAt,
                Status = CurrentStatus ?? string.Empty,
                Items = new List<OrderItemDto>
                {
                    new()
                    {
                        SourceItemId = "primary",
                        ProductCode = ServiceCategoryName,
                        ProductName = productName,
                        Quantity = quantity,
                        UnitOfMeasure = "EA",
                        UnitPrice = unitPrice,
                        TotalPrice = QuotedAmount.GetValueOrDefault(),
                        Currency = QuoteCurrency ?? "THB",
                        Notes = Requirements
                    }
                }
            };
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.First(value => !string.IsNullOrWhiteSpace(value))!;
        }
    }
}
