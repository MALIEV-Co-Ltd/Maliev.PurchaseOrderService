using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Clients;

/// <summary>
/// Client interface for Order Service
/// </summary>
public interface IOrderServiceClient
{
    /// <summary>
    /// Get order items by order ID
    /// </summary>
    Task<List<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get order items summary by order ID
    /// </summary>
    Task<OrderItemsSummaryDto> GetOrderItemsSummaryAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh order items from source system
    /// </summary>
    Task<OrderItemRefreshResult> RefreshOrderItemsAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if order exists
    /// </summary>
    Task<bool> OrderExistsAsync(int orderId, CancellationToken cancellationToken = default);
}