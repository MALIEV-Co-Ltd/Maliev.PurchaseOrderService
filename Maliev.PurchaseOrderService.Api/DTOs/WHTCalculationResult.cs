namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of withholding tax calculation
/// </summary>
public class WHTCalculationResult
{
    /// <summary>
    /// Whether WHT is applicable for this transaction
    /// </summary>
    public bool IsApplicable { get; set; }

    /// <summary>
    /// WHT rate applied (as decimal, e.g., 0.03 for 3%)
    /// </summary>
    public decimal WHTRate { get; set; }

    /// <summary>
    /// Calculated WHT amount in original currency
    /// </summary>
    public decimal WHTAmount { get; set; }

    /// <summary>
    /// Final amount after WHT deduction
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Subtotal amount before WHT
    /// </summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Tax base amount used for WHT calculation
    /// </summary>
    public decimal TaxBase { get; set; }

    /// <summary>
    /// Currency code used for calculation
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// WHT amount in THB for tax reporting
    /// </summary>
    public decimal WHTAmountTHB { get; set; }

    /// <summary>
    /// Exchange rate used for THB conversion (if applicable)
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Reason why WHT is applied or not applied
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// WHT certificate number (generated for applicable transactions)
    /// </summary>
    public string? WHTCertificateNumber { get; set; }

    /// <summary>
    /// Tax regulation applied for this calculation
    /// </summary>
    public string? TaxRegulation { get; set; }
}