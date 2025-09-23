using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Interface for purchase order business logic service
/// </summary>
public interface IPurchaseOrderService
{
    /// <summary>
    /// Gets all purchase orders with optional filtering and pagination
    /// </summary>
    /// <param name="searchRequest">Search and filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated purchase orders</returns>
    Task<PaginatedResponse<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        SearchPurchaseOrdersRequest searchRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a purchase order by ID
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase order or null if not found</returns>
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new purchase order
    /// </summary>
    /// <param name="request">Create purchase order request</param>
    /// <param name="createdBy">User creating the purchase order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created purchase order</returns>
    Task<PurchaseOrderDto> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        string createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Update purchase order request</param>
    /// <param name="lastModifiedBy">User updating the purchase order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated purchase order or null if not found</returns>
    Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(
        int id,
        UpdatePurchaseOrderRequest request,
        string lastModifiedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a purchase order (soft delete)
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="deletedBy">User performing the delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeletePurchaseOrderAsync(
        int id,
        string deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated purchase order or null if not found</returns>
    Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(
        int id,
        ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated purchase order or null if not found</returns>
    Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(
        int id,
        CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase order summary statistics
    /// </summary>
    /// <param name="userId">Optional user ID to filter by user's orders</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase order statistics</returns>
    Task<PurchaseOrderStatsDto> GetPurchaseOrderStatsAsync(
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a purchase order for business rules
    /// </summary>
    /// <param name="request">Purchase order request to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase orders by customer PO number
    /// </summary>
    /// <param name="customerPoNumber">Customer PO number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase orders matching the customer PO number</returns>
    Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByCustomerPoAsync(
        string customerPoNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates WHT for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">WHT calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WHT calculation result</returns>
    Task<WHTCalculationResult?> CalculateWHTAsync(
        int id,
        WHTCalculationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets WHT calculation history for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of WHT calculations</returns>
    Task<List<WHTCalculationResult>?> GetWHTHistoryAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates WHT for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated purchase order with recalculated WHT</returns>
    Task<PurchaseOrderDto?> RecalculateWHTAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets purchase order change history
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit log entries</returns>
    Task<IEnumerable<AuditLogDto>> GetPurchaseOrderHistoryAsync(
        int id,
        CancellationToken cancellationToken = default);
}