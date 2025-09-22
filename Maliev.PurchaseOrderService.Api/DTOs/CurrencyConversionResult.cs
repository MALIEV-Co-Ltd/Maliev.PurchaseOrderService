namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of currency conversion
/// </summary>
public class CurrencyConversionResult
{
    /// <summary>
    /// Source currency code
    /// </summary>
    public string FromCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code
    /// </summary>
    public string ToCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Original amount in source currency
    /// </summary>
    public decimal OriginalAmount { get; set; }

    /// <summary>
    /// Converted amount in target currency
    /// </summary>
    public decimal ConvertedAmount { get; set; }

    /// <summary>
    /// Exchange rate used for conversion
    /// </summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// When the conversion was performed
    /// </summary>
    public DateTime ConvertedAt { get; set; }

    /// <summary>
    /// Rate source (e.g., "Bank of Thailand", "API Provider")
    /// </summary>
    public string RateSource { get; set; } = string.Empty;

    /// <summary>
    /// Conversion fees if applicable
    /// </summary>
    public decimal? ConversionFee { get; set; }

    /// <summary>
    /// Net amount after fees
    /// </summary>
    public decimal? NetAmount { get; set; }

    /// <summary>
    /// Rate validity timestamp
    /// </summary>
    public DateTime? RateValidUntil { get; set; }
}