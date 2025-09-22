using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for currency conversion
/// </summary>
public class CurrencyConversionRequest
{
    /// <summary>
    /// Source currency code
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string FromCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string ToCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Amount to convert
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Specific date for historical conversion rates
    /// </summary>
    public DateTime? ConversionDate { get; set; }

    /// <summary>
    /// Use real-time rates instead of cached rates
    /// </summary>
    public bool UseRealTimeRates { get; set; } = false;
}