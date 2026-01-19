using Maliev.PurchaseOrderService.Common.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to create a new purchase order
/// </summary>
public class CreatePurchaseOrderRequest
{
    /// <summary>
    /// ID of the supplier in SupplierService
    /// </summary>
    [Required]
    public int SupplierID { get; set; }

    /// <summary>
    /// ID of the order/quotation in OrderService
    /// </summary>
    [Required]
    public int OrderID { get; set; }

    /// <summary>
    /// ID of the currency in CurrencyService
    /// </summary>
    [Required]
    public int CurrencyID { get; set; }

    /// <summary>
    /// Type of purchase order (Internal or External)
    /// </summary>
    [Required]
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Optional customer purchase order number for external POs (all characters allowed)
    /// </summary>
    [MaxLength(50)]
    public string? CustomerPO { get; set; }

    /// <summary>
    /// Optional array for partial ordering. If provided, only these items are included in the PO.
    /// If omitted, all items from the quotation are fetched.
    /// </summary>
    public List<PartialOrderItem>? Items { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Withholding tax rate percentage (0.00-99.99)
    /// </summary>
    [Range(0.00, 99.99)]
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Shipping address for the purchase order
    /// </summary>
    public CreateAddressRequest? ShippingAddress { get; set; }

    /// <summary>
    /// Billing address for the purchase order
    /// </summary>
    public CreateAddressRequest? BillingAddress { get; set; }
}

/// <summary>
/// Represents a partial order item for selective ordering
/// </summary>
public class PartialOrderItem
{
    /// <summary>
    /// Reference to the item ID in OrderService/QuotationService
    /// </summary>
    [Required]
    public int ExternalOrderItemId { get; set; }

    /// <summary>
    /// Quantity for this item (can differ from quotation quantity)
    /// </summary>
    [Required]
    [Range(0.001, double.MaxValue)]
    public decimal Quantity { get; set; }
}
