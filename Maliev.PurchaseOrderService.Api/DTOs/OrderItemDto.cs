using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Order item data transfer object for API operations
/// Individual line items derived from OrderService/QuotationService for the referenced order.
/// Read-only entity populated from external services.
/// </summary>
public class OrderItemDto
{
    /// <summary>
    /// Local unique identifier for caching
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to parent PurchaseOrder
    /// </summary>
    [Required]
    public int PurchaseOrderId { get; set; }

    /// <summary>
    /// Reference to the original item in OrderService
    /// </summary>
    [Required]
    public int ExternalOrderItemId { get; set; }

    /// <summary>
    /// Product/SKU code from external service
    /// </summary>
    [StringLength(50)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Name/description from external service
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity from external order
    /// </summary>
    [Required]
    [Range(0.001, 999999.999)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure from external service
    /// </summary>
    [Required]
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    /// <summary>
    /// Price per unit from external service
    /// </summary>
    [Required]
    [Range(0, 999999999999.99)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Calculated total (Quantity × UnitPrice)
    /// </summary>
    [Required]
    [Range(0, 999999999999.99)]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Currency code (inherited from PurchaseOrder)
    /// </summary>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Expected delivery date for this item
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Item-specific notes from external service
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// When this data was last retrieved from external service
    /// </summary>
    [Required]
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Flag indicating if external item has been modified since caching
    /// </summary>
    [Required]
    public bool ExternallyModified { get; set; } = false;
}

/// <summary>
/// Order item request for creating new order items
/// </summary>
public class CreateOrderItemRequest
{
    /// <summary>
    /// Product/SKU code
    /// </summary>
    [StringLength(50)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product name/description
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    [Required]
    [Range(0.001, 999999.999)]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = "pcs";

    /// <summary>
    /// Price per unit
    /// </summary>
    [Required]
    [Range(0, 999999999999.99)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Expected delivery date for this item
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Item-specific notes
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Order item request for updating existing order items
/// </summary>
public class UpdateOrderItemRequest
{
    /// <summary>
    /// Product/SKU code
    /// </summary>
    [StringLength(50)]
    public string? ProductCode { get; set; }

    /// <summary>
    /// Product name/description
    /// </summary>
    [StringLength(200)]
    public string? ProductName { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    [Range(0.001, 999999.999)]
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    [StringLength(20)]
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Price per unit
    /// </summary>
    [Range(0, 999999999999.99)]
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Expected delivery date for this item
    /// </summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>
    /// Item-specific notes
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; set; }
}