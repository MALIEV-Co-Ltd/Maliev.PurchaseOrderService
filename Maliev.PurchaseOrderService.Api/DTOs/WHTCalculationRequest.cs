using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Api.Attributes;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for calculating withholding tax (WHT) for a purchase order
/// Used for Thailand tax compliance calculations
/// </summary>
public class WHTCalculationRequest
{
    /// <summary>
    /// Subtotal amount before WHT calculation
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Subtotal amount must be greater than 0")]
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Currency code for the calculation
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be exactly 3 characters")]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Supplier ID for determining WHT rules
    /// </summary>
    [Required]
    public int SupplierID { get; set; }

    /// <summary>
    /// Order type (Internal/External) affects WHT calculation
    /// </summary>
    [Required]
    public Data.Enums.OrderType OrderType { get; set; }

    /// <summary>
    /// Optional explicit WHT rate to apply (overrides system calculation)
    /// </summary>
    [WHTRateValidation]
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Purchase order ID for audit trail
    /// </summary>
    public int? PurchaseOrderId { get; set; }

    /// <summary>
    /// Additional context for WHT calculation
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Supplier country for tax regulation determination
    /// </summary>
    [MaxLength(100)]
    public string? SupplierCountry { get; set; }

    /// <summary>
    /// Total amount for WHT calculation (alternative to SubtotalAmount)
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Service type for WHT calculation (e.g., "Professional Services", "Consulting")
    /// </summary>
    [MaxLength(100)]
    public string? ServiceType { get; set; }
}