using Maliev.PurchaseOrderService.Common.Enumerations;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for searching and filtering purchase orders
/// </summary>
public class SearchPurchaseOrdersRequest
{
    /// <summary>
    /// Search term for PO number, customer PO number, or description
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by supplier ID
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Filter by order ID
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Filter by currency code
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Filter by customer PO number
    /// </summary>
    public string? CustomerPoNumber { get; set; }

    /// <summary>
    /// Filter by order status
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Filter by order type
    /// </summary>
    public OrderType? OrderType { get; set; }

    /// <summary>
    /// Filter by created user
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Filter by minimum total amount
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Filter by maximum total amount
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Filter by created date from
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by created date to
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter by updated date from
    /// </summary>
    public DateTime? UpdatedFrom { get; set; }

    /// <summary>
    /// Filter by updated date to
    /// </summary>
    public DateTime? UpdatedTo { get; set; }

    /// <summary>
    /// Filter by expected delivery date from
    /// </summary>
    public DateTime? ExpectedDeliveryFrom { get; set; }

    /// <summary>
    /// Filter by expected delivery date to
    /// </summary>
    public DateTime? ExpectedDeliveryTo { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public PurchaseOrderSortType SortBy { get; set; } = PurchaseOrderSortType.CreatedAt;

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Include soft-deleted records
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;

    /// <summary>
    /// Include order items in results
    /// </summary>
    public bool IncludeOrderItems { get; set; } = false;

    /// <summary>
    /// Include addresses in results
    /// </summary>
    public bool IncludeAddresses { get; set; } = false;
}