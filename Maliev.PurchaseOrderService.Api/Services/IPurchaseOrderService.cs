using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for managing purchase orders
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// Create a new purchase order
    /// </summary>
    Task<PurchaseOrderDetailResponse> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a purchase order by ID
    /// </summary>
    Task<PurchaseOrderDetailResponse?> GetPurchaseOrderByIdAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for purchase orders with filtering and pagination
    /// </summary>
    Task<PaginatedResponse<PurchaseOrderResponse>> SearchPurchaseOrdersAsync(SearchPurchaseOrdersRequest request, string userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing purchase order
    /// </summary>
    Task<PurchaseOrderDetailResponse> UpdatePurchaseOrderAsync(int id, UpdatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a purchase order
    /// </summary>
    Task CancelPurchaseOrderAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default);
}
