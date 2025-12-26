using Maliev.PurchaseOrderService.Common.Enumerations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Purchase order summary response
/// </summary>
public class PurchaseOrderResponse
{
    /// <summary>
    /// Unique identifier for the purchase order.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Auto-generated unique internal order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>
    /// Optional customer purchase order number for external POs.
    /// </summary>
    public string? CustomerPO { get; set; }
    /// <summary>
    /// ID of the supplier in SupplierService.
    /// </summary>
    public int SupplierID { get; set; }
    /// <summary>
    /// Cached supplier name from SupplierService.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;
    /// <summary>
    /// Cached contact information for the supplier.
    /// </summary>
    public string? SupplierContactInfo { get; set; }
    /// <summary>
    /// ID of the order/quotation in OrderService.
    /// </summary>
    public int OrderID { get; set; }
    /// <summary>
    /// ID of the currency in CurrencyService.
    /// </summary>
    public int CurrencyID { get; set; }
    /// <summary>
    /// Cached currency code from CurrencyService (e.g., "THB", "USD").
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>
    /// Cached currency symbol for display.
    /// </summary>
    public string CurrencySymbol { get; set; } = string.Empty;
    /// <summary>
    /// Date the order was created/issued.
    /// </summary>
    public DateTime OrderDate { get; set; }
    /// <summary>
    /// Expected delivery date for the order.
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }
    /// <summary>
    /// Current status of the order.
    /// </summary>
    public OrderStatus Status { get; set; }
    /// <summary>
    /// Type of the order (Internal or External).
    /// </summary>
    public OrderType OrderType { get; set; }
    /// <summary>
    /// Subtotal amount before WHT calculated from derived order items.
    /// </summary>
    public decimal SubtotalAmount { get; set; }
    /// <summary>
    /// Withholding tax rate percentage.
    /// </summary>
    public decimal? WHTRate { get; set; }
    /// <summary>
    /// Calculated withholding tax amount.
    /// </summary>
    public decimal? WHTAmount { get; set; }
    /// <summary>
    /// Final total amount after WHT deduction.
    /// </summary>
    public decimal TotalAmount { get; set; }
    /// <summary>
    /// User ID who created the order.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
    /// <summary>
    /// Date and time when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// User ID who last modified the order.
    /// </summary>
    public string? LastModifiedBy { get; set; }
    /// <summary>
    /// Date and time when the order was last modified.
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }
    /// <summary>
    /// User ID who approved the order.
    /// </summary>
    public string? ApprovedBy { get; set; }
    /// <summary>
    /// Date and time when the order was approved.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }
    /// <summary>
    /// Additional notes or comments.
    /// </summary>
    public string? Notes { get; set; }
    /// <summary>
    /// Concurrency token (Base64 encoded row version).
    /// </summary>
    public string RowVersion { get; set; } = string.Empty; // Base64 encoded
    /// <summary>
    /// Indicates if the purchase order data is stale.
    /// </summary>
    public bool IsStale { get; set; }
}
