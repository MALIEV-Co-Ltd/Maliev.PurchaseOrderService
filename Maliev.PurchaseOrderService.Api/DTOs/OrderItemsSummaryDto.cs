namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Summary statistics for order items
/// </summary>
public class OrderItemsSummaryDto
{
    /// <summary>
    /// Purchase order ID
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Total number of order items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total quantity across all items
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total value across all items
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Number of unique item categories
    /// </summary>
    public int UniqueCategories { get; set; }

    /// <summary>
    /// When the items were last updated
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Breakdown by category
    /// </summary>
    public List<CategorySummaryDto> CategoryBreakdown { get; set; } = new();
}

/// <summary>
/// Summary for a specific item category
/// </summary>
public class CategorySummaryDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Number of items in this category
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Total quantity for this category
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total value for this category
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Percentage of total value
    /// </summary>
    public decimal ValuePercentage { get; set; }
}