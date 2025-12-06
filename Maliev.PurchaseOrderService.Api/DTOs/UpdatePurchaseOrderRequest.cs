using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to update an existing purchase order
/// </summary>
public class UpdatePurchaseOrderRequest
{
    /// <summary>
    /// Currency ID (optional, triggers recalculation)
    /// </summary>
    public int? CurrencyID { get; set; }

    /// <summary>
    /// Customer PO number (all characters allowed)
    /// </summary>
    [MaxLength(50)]
    public string? CustomerPO { get; set; }

    /// <summary>
    /// Optional array for partial ordering
    /// </summary>
    public List<PartialOrderItem>? Items { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Withholding tax rate (triggers recalculation)
    /// </summary>
    [Range(0.00, 99.99)]
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Shipping address
    /// </summary>
    public UpdateAddressRequest? ShippingAddress { get; set; }

    /// <summary>
    /// Billing address
    /// </summary>
    public UpdateAddressRequest? BillingAddress { get; set; }

    /// <summary>
    /// Concurrency token (base64 encoded row version)
    /// </summary>
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
