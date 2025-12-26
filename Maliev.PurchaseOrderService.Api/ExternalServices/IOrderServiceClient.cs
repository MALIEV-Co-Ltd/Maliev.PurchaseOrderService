namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Client for interacting with OrderService
/// </summary>
public interface IOrderServiceClient
{
    /// <summary>
    /// Retrieves an order by its ID asynchronously.
    /// </summary>
    /// <param name="orderId">The ID of the order to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="OrderDto"/> if found, otherwise null.</returns>
    Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves order items for a specific order asynchronously.
    /// </summary>
    /// <param name="orderId">The ID of the order.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="OrderItemDto"/>.</returns>
    Task<List<OrderItemDto>> GetOrderItemsAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if an order with the specified ID exists asynchronously.
    /// </summary>
    /// <param name="orderId">The ID of the order to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the order exists, false otherwise.</returns>
    Task<bool> ValidateOrderExistsAsync(int orderId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for order information.
/// </summary>
public class OrderDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the order.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the date the order was placed.
    /// </summary>
    public DateTime OrderDate { get; set; }
    /// <summary>
    /// Gets or sets the current status of the order.
    /// </summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the list of items in the order.
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();
}

/// <summary>
/// Data transfer object for order item information.
/// </summary>
public class OrderItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the order item.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Gets or sets the product code.
    /// </summary>
    public string? ProductCode { get; set; }
    /// <summary>
    /// Gets or sets the name of the product.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the quantity of the product.
    /// </summary>
    public decimal Quantity { get; set; }
    /// <summary>
    /// Gets or sets the unit of measure for the product.
    /// </summary>
    public string UnitOfMeasure { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the unit price of the product.
    /// </summary>
    public decimal UnitPrice { get; set; }
    /// <summary>
    /// Gets or sets the total price for the order item.
    /// </summary>
    public decimal TotalPrice { get; set; }
    /// <summary>
    /// Gets or sets the currency of the price.
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets additional notes for the order item.
    /// </summary>
    public string? Notes { get; set; }
}
