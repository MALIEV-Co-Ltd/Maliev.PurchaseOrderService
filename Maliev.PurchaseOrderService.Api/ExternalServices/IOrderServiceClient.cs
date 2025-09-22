using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Interface for Order Service external API client
/// </summary>
public interface IOrderServiceClient
{
    /// <summary>
    /// Gets order information by ID
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order information or null if not found</returns>
    Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order status by ID
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order status information or null if not found</returns>
    Task<OrderStatusDto?> GetOrderStatusAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates order status
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="status">The new status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if update was successful</returns>
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order items for a specific order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of order items</returns>
    Task<IEnumerable<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an order exists and is in a valid state for purchase order creation
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if order is valid for purchase order creation</returns>
    Task<bool> ValidateOrderForPurchaseOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order delivery information
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order delivery information or null if not found</returns>
    Task<OrderDeliveryDto?> GetOrderDeliveryInfoAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a purchase order to an order
    /// </summary>
    /// <param name="orderId">The order ID</param>
    /// <param name="purchaseOrderId">The purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if linking was successful</returns>
    Task<bool> LinkPurchaseOrderAsync(int orderId, int purchaseOrderId, CancellationToken cancellationToken = default);
}