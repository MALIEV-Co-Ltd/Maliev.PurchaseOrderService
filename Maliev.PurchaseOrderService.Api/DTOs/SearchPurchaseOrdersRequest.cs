using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to search and filter purchase orders
/// </summary>
public class SearchPurchaseOrdersRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by order status
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Filter by order type
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// Filter by supplier ID
    /// </summary>
    public int? SupplierID { get; set; }

    /// <summary>
    /// Filter by order ID
    /// </summary>
    public int? OrderID { get; set; }

    /// <summary>
    /// Filter by creation date from
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by creation date to
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}
