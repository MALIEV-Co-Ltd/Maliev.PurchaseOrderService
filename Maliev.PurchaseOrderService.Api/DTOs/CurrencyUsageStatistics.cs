namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Statistics about currency usage in purchase orders
/// </summary>
public class CurrencyUsageStatistics
{
    /// <summary>
    /// Currency code
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency name
    /// </summary>
    public string CurrencyName { get; set; } = string.Empty;

    /// <summary>
    /// Number of purchase orders using this currency
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Total value in this currency
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Percentage of total purchase orders
    /// </summary>
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// Average order value in this currency
    /// </summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>
    /// Last used date
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Trend indicator (increasing, stable, decreasing)
    /// </summary>
    public string Trend { get; set; } = "stable";
}