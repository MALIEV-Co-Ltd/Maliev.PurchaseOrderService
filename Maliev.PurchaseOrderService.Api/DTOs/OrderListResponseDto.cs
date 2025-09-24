namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Response containing a list of orders
/// </summary>
public class OrderListResponseDto
{
    /// <summary>
    /// List of orders
    /// </summary>
    public List<OrderDto> Orders { get; set; } = new();

    /// <summary>
    /// Total count of orders matching criteria
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size used in query
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}