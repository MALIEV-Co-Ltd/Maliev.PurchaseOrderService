using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// HTTP client implementation for Order Service integration
/// Handles order items derivation and read-only operations
/// </summary>
public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;
    private readonly ExternalServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public OrderServiceClient(
        HttpClient httpClient,
        ILogger<OrderServiceClient> logger,
        IOptions<ExternalServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order information for ID: {OrderId}", orderId);

            var response = await _httpClient.GetAsync($"/orders/{orderId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order not found for ID: {OrderId}", orderId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var order = JsonSerializer.Deserialize<OrderDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved order information for ID: {OrderId}", orderId);
            return order;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting order {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to get order {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting order {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while getting order {orderId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting order {OrderId}", orderId);
            throw new ExternalServiceException($"Invalid response format while getting order {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrderStatusDto?> GetOrderStatusAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order status for ID: {OrderId}", orderId);

            var response = await _httpClient.GetAsync($"/orders/{orderId}/status", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order status not found for ID: {OrderId}", orderId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var orderStatus = JsonSerializer.Deserialize<OrderStatusDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved order status for ID: {OrderId}", orderId);
            return orderStatus;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting order status {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to get order status {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting order status {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while getting order status {orderId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting order status {OrderId}", orderId);
            throw new ExternalServiceException($"Invalid response format while getting order status {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating order status for ID: {OrderId} to {Status}", orderId, status);

            var requestBody = new { Status = status };
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"/orders/{orderId}/status", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order not found for status update, ID: {OrderId}", orderId);
                return false;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully updated order status for ID: {OrderId} to {Status}", orderId, status);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while updating order status {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to update order status {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while updating order status {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while updating order status {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order items for ID: {OrderId}", orderId);

            var response = await _httpClient.GetAsync($"/orders/{orderId}/items", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order items not found for ID: {OrderId}", orderId);
                return Enumerable.Empty<OrderItemDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var orderItems = JsonSerializer.Deserialize<IEnumerable<OrderItemDto>>(content, _jsonOptions) ??
                            Enumerable.Empty<OrderItemDto>();

            _logger.LogInformation("Successfully retrieved {ItemCount} order items for ID: {OrderId}",
                orderItems.Count(), orderId);
            return orderItems;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting order items {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to get order items {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting order items {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while getting order items {orderId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting order items {OrderId}", orderId);
            throw new ExternalServiceException($"Invalid response format while getting order items {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateOrderForPurchaseOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating order for purchase order creation, ID: {OrderId}", orderId);

            var response = await _httpClient.GetAsync($"/orders/{orderId}/validate-for-purchase-order", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order validation failed - not found for ID: {OrderId}", orderId);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<Dictionary<string, bool>>(content, _jsonOptions);
                var isValid = result?.GetValueOrDefault("isValid", false) ?? false;

                _logger.LogInformation("Order validation result for ID {OrderId}: {IsValid}", orderId, isValid);
                return isValid;
            }

            response.EnsureSuccessStatusCode();
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating order {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to validate order {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while validating order {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while validating order {orderId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while validating order {OrderId}", orderId);
            throw new ExternalServiceException($"Invalid response format while validating order {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrderDeliveryDto?> GetOrderDeliveryInfoAsync(int orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order delivery info for ID: {OrderId}", orderId);

            var response = await _httpClient.GetAsync($"/orders/{orderId}/delivery", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order delivery info not found for ID: {OrderId}", orderId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var deliveryInfo = JsonSerializer.Deserialize<OrderDeliveryDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved order delivery info for ID: {OrderId}", orderId);
            return deliveryInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting order delivery info {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to get order delivery info {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting order delivery info {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while getting order delivery info {orderId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting order delivery info {OrderId}", orderId);
            throw new ExternalServiceException($"Invalid response format while getting order delivery info {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> LinkPurchaseOrderAsync(int orderId, int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Linking purchase order {PurchaseOrderId} to order {OrderId}", purchaseOrderId, orderId);

            var requestBody = new { PurchaseOrderId = purchaseOrderId };
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/orders/{orderId}/link-purchase-order", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order not found for purchase order linking, ID: {OrderId}", orderId);
                return false;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully linked purchase order {PurchaseOrderId} to order {OrderId}",
                purchaseOrderId, orderId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while linking purchase order to order {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to link purchase order to order {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while linking purchase order to order {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while linking purchase order to order {orderId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(int customerId, string? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting orders by customer ID: {CustomerId}, Status: {Status}", customerId, status ?? "All");

            var queryParams = new List<string> { $"customerId={customerId}" };
            if (!string.IsNullOrWhiteSpace(status))
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/orders?{queryString}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No orders found for customer ID: {CustomerId}", customerId);
                return Enumerable.Empty<OrderDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var ordersResponse = JsonSerializer.Deserialize<OrderListResponseDto>(content, _jsonOptions);
            var orders = ordersResponse?.Orders ?? Enumerable.Empty<OrderDto>();

            _logger.LogInformation("Successfully retrieved {OrderCount} orders for customer ID: {CustomerId}",
                orders.Count(), customerId);
            return orders;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting orders by customer {CustomerId}", customerId);
            throw new ExternalServiceException($"Failed to get orders by customer {customerId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting orders by customer {CustomerId}", customerId);
            throw new ExternalServiceException($"Timeout while getting orders by customer {customerId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting orders by customer {CustomerId}", customerId);
            throw new ExternalServiceException($"Invalid response format while getting orders by customer {customerId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest createRequest, CancellationToken cancellationToken = default)
    {
        try
        {
            if (createRequest == null)
            {
                throw new ArgumentNullException(nameof(createRequest));
            }

            _logger.LogInformation("Creating order for customer ID: {CustomerId}", createRequest.CustomerId);

            var jsonContent = JsonSerializer.Serialize(createRequest, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/orders", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Bad request while creating order: {ErrorContent}", errorContent);
                throw new ExternalServiceException($"Bad request while creating order: {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var createdOrder = JsonSerializer.Deserialize<OrderDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully created order with ID: {OrderId}", createdOrder?.Id);
            return createdOrder;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while creating order");
            throw new ExternalServiceException($"Failed to create order: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while creating order");
            throw new ExternalServiceException("Timeout while creating order", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error while creating order");
            throw new ExternalServiceException("Invalid request format while creating order", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> CancelOrderAsync(int orderId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Canceling order ID: {OrderId}, Reason: {Reason}", orderId, reason);

            var requestBody = new { Reason = reason, CancelledBy = "system" };
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/orders/{orderId}/cancel", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Order not found for cancellation, ID: {OrderId}", orderId);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Successfully canceled order ID: {OrderId}", orderId);
                return true;
            }

            response.EnsureSuccessStatusCode();
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while canceling order {OrderId}", orderId);
            throw new ExternalServiceException($"Failed to cancel order {orderId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while canceling order {OrderId}", orderId);
            throw new ExternalServiceException($"Timeout while canceling order {orderId}", ex);
        }
    }
}