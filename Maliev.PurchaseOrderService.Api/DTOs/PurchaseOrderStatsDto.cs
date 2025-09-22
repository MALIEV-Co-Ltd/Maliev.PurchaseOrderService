namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Purchase order statistics
/// </summary>
public class PurchaseOrderStatsDto
{
    /// <summary>
    /// Total number of purchase orders
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of draft purchase orders
    /// </summary>
    public int DraftCount { get; set; }

    /// <summary>
    /// Number of pending approval purchase orders
    /// </summary>
    public int PendingApprovalCount { get; set; }

    /// <summary>
    /// Number of approved purchase orders
    /// </summary>
    public int ApprovedCount { get; set; }

    /// <summary>
    /// Number of canceled purchase orders
    /// </summary>
    public int CanceledCount { get; set; }

    /// <summary>
    /// Total value of all purchase orders
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Total value of approved purchase orders
    /// </summary>
    public decimal ApprovedValue { get; set; }

    /// <summary>
    /// Total WHT amount across all purchase orders
    /// </summary>
    public decimal TotalWHTAmount { get; set; }

    /// <summary>
    /// Number of internal purchase orders
    /// </summary>
    public int InternalCount { get; set; }

    /// <summary>
    /// Number of external purchase orders
    /// </summary>
    public int ExternalCount { get; set; }

    /// <summary>
    /// Statistics breakdown by month (last 12 months)
    /// </summary>
    public List<MonthlyStatsDto> MonthlyStats { get; set; } = new();

    /// <summary>
    /// Statistics breakdown by currency
    /// </summary>
    public List<CurrencyStatsDto> CurrencyStats { get; set; } = new();

    /// <summary>
    /// Statistics breakdown by supplier (top 10)
    /// </summary>
    public List<SupplierStatsDto> TopSuppliers { get; set; } = new();

    /// <summary>
    /// Statistics generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// User ID for filtered statistics (if applicable)
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Monthly statistics
/// </summary>
public class MonthlyStatsDto
{
    /// <summary>
    /// Year and month (YYYY-MM)
    /// </summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>
    /// Number of purchase orders created in this month
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total value of purchase orders created in this month
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Average value of purchase orders in this month
    /// </summary>
    public decimal AverageValue { get; set; }
}

/// <summary>
/// Currency statistics
/// </summary>
public class CurrencyStatsDto
{
    /// <summary>
    /// Currency code
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of purchase orders in this currency
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total value in original currency
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Total value converted to THB
    /// </summary>
    public decimal TotalValueTHB { get; set; }

    /// <summary>
    /// Percentage of total purchase orders
    /// </summary>
    public decimal Percentage { get; set; }
}

/// <summary>
/// Supplier statistics
/// </summary>
public class SupplierStatsDto
{
    /// <summary>
    /// Supplier ID
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Supplier name (if available)
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Number of purchase orders for this supplier
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total value of purchase orders for this supplier
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Average value of purchase orders for this supplier
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    /// Percentage of total purchase orders
    /// </summary>
    public decimal Percentage { get; set; }
}