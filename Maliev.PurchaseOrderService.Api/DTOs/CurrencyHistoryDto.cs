namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Currency change history for purchase orders
/// </summary>
public class CurrencyHistoryDto
{
    /// <summary>
    /// Unique identifier for the history entry
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Purchase order ID
    /// </summary>
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Currency code that was used
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency name
    /// </summary>
    public string CurrencyName { get; set; } = string.Empty;

    /// <summary>
    /// Exchange rate at the time of change
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Amount in this currency
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When this currency was set
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Who made the currency change
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Reason for currency change
    /// </summary>
    public string? ChangeReason { get; set; }
}