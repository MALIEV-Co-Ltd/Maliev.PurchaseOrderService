using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Clients;

/// <summary>
/// HTTP client for Order Service
/// </summary>
public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<List<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/orders/{orderId}/items", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get order items for order {OrderId}. Status: {StatusCode}",
                    orderId, response.StatusCode);
                return new List<OrderItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var items = JsonSerializer.Deserialize<List<OrderItemDto>>(content, _jsonOptions);

            return items ?? new List<OrderItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order items for order {OrderId}", orderId);
            return new List<OrderItemDto>();
        }
    }

    /// <inheritdoc />
    public async Task<OrderItemsSummaryDto> GetOrderItemsSummaryAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/orders/{orderId}/items/summary", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get order items summary for order {OrderId}. Status: {StatusCode}",
                    orderId, response.StatusCode);
                return new OrderItemsSummaryDto();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var summary = JsonSerializer.Deserialize<OrderItemsSummaryDto>(content, _jsonOptions);

            return summary ?? new OrderItemsSummaryDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order items summary for order {OrderId}", orderId);
            return new OrderItemsSummaryDto();
        }
    }

    /// <inheritdoc />
    public async Task<OrderItemRefreshResult> RefreshOrderItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/orders/{orderId}/items/refresh", null, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to refresh order items for order {OrderId}. Status: {StatusCode}",
                    orderId, response.StatusCode);
                return new OrderItemRefreshResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to refresh order items. Status: {response.StatusCode}"
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OrderItemRefreshResult>(content, _jsonOptions);

            return result ?? new OrderItemRefreshResult { Success = false, ErrorMessage = "Invalid response" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing order items for order {OrderId}", orderId);
            return new OrderItemRefreshResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> OrderExistsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/orders/{orderId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if order {OrderId} exists", orderId);
            return false;
        }
    }
}