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
        try
        {
            // Base URL already includes /orders/v1/, so just request {id}
            var response = await _httpClient.GetAsync($"{orderId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch order {OrderId}: {StatusCode}", orderId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
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
            // Base URL already includes /orders/v1/, so request {id}/items
            var response = await _httpClient.GetAsync($"{orderId}/items", cancellationToken);

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
}
