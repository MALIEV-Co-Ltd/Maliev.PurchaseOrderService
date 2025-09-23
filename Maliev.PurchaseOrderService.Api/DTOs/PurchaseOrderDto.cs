using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.OpenApi.Models;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Complete purchase order data transfer object for API operations
/// </summary>
public class PurchaseOrderDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Auto-generated unique internal order number (e.g., "PO-2025-001234")
    /// </summary>
    [Required]
    [StringLength(20)]
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional customer purchase order number for external POs
    /// </summary>
    [StringLength(50)]
    public string? CustomerPO { get; set; }

    /// <summary>
    /// References a supplier in SupplierService
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "SupplierID must be a positive integer")]
    public int SupplierID { get; set; }

    /// <summary>
    /// References an order/quotation/internal order in OrderService
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "OrderID must be a positive integer")]
    public int OrderID { get; set; }

    /// <summary>
    /// References a currency in CurrencyService
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "CurrencyID must be a positive integer")]
    public int CurrencyID { get; set; }

    /// <summary>
    /// Cached supplier name from SupplierService
    /// </summary>
    [Required]
    [StringLength(100)]
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Cached contact information
    /// </summary>
    [StringLength(200)]
    public string? SupplierContactInfo { get; set; }

    /// <summary>
    /// Cached currency code from CurrencyService (e.g., "THB", "USD")
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Cached currency symbol for display
    /// </summary>
    [Required]
    [StringLength(10)]
    public string CurrencySymbol { get; set; } = string.Empty;

    /// <summary>
    /// Date when the purchase order was created
    /// </summary>
    [Required]
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Expected delivery date
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Order status (Pending, Approved, Ordered, Delivered, Cancelled)
    /// </summary>
    [Required]
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Internal (company operations) or External (client projects)
    /// </summary>
    [Required]
    public OrderType OrderType { get; set; }

    /// <summary>
    /// Subtotal amount before WHT calculated from derived order items
    /// </summary>
    [Required]
    [Range(0, 999999999999.99)]
    public decimal SubtotalAmount { get; set; }

    /// <summary>
    /// Withholding tax rate percentage (0.00-99.99%)
    /// </summary>
    [Range(0.00, 99.99)]
    public decimal? WHTRate { get; set; }

    /// <summary>
    /// Calculated withholding tax amount
    /// </summary>
    [Range(0, 999999999999.99)]
    public decimal? WHTAmount { get; set; }

    /// <summary>
    /// Final total amount after WHT deduction (SubtotalAmount - WHTAmount)
    /// </summary>
    [Required]
    [Range(0, 999999999999.99)]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "EUR") - duplicate for compatibility
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// User ID who created the order
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who last modified the order
    /// </summary>
    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who approved the order
    /// </summary>
    [StringLength(50)]
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Approval timestamp
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// User ID who cancelled the order
    /// </summary>
    [StringLength(50)]
    public string? CancelledBy { get; set; }

    /// <summary>
    /// Cancellation timestamp
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Additional notes or comments
    /// </summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Optimistic concurrency control token (base64 encoded)
    /// </summary>
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key for shipping address
    /// </summary>
    public int? ShippingAddressId { get; set; }

    /// <summary>
    /// Foreign key for billing address
    /// </summary>
    public int? BillingAddressId { get; set; }

    /// <summary>
    /// Shipping address information
    /// </summary>
    public AddressDto? ShippingAddress { get; set; }

    /// <summary>
    /// Billing address information
    /// </summary>
    public AddressDto? BillingAddress { get; set; }

    /// <summary>
    /// Individual line items derived from OrderService/QuotationService
    /// </summary>
    public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();

    /// <summary>
    /// Documents uploaded for this purchase order
    /// </summary>
    public ICollection<PurchaseOrderFileDto> PurchaseOrderFiles { get; set; } = new List<PurchaseOrderFileDto>();
}

