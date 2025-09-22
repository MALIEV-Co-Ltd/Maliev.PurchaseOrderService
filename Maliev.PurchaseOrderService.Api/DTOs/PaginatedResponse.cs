namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Generic paginated response
/// </summary>
/// <typeparam name="T">Type of data items</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Data items for current page
    /// </summary>
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Applied filters and search criteria
    /// </summary>
    public object? Filters { get; set; }

    /// <summary>
    /// Sort information
    /// </summary>
    public object? Sort { get; set; }
}