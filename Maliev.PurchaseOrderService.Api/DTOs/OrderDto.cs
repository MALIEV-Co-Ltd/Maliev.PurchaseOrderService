namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Data Transfer Object for Order information
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public AddressDto? BillingAddress { get; set; }
    public AddressDto? ShippingAddress { get; set; }
    public List<OrderItemDto> OrderItems { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Data Transfer Object for Order status information
/// </summary>
public class OrderStatusDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime StatusDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public bool CanCreatePurchaseOrder { get; set; }
    public bool CanModify { get; set; }
    public bool CanCancel { get; set; }
}

/// <summary>
/// Data Transfer Object for Order delivery information
/// </summary>
public class OrderDeliveryDto
{
    public int OrderId { get; set; }
    public string DeliveryMethod { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingCarrier { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string ShippingCurrency { get; set; } = string.Empty;
    public DateTime? EstimatedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public AddressDto? DeliveryAddress { get; set; }
    public string DeliveryInstructions { get; set; } = string.Empty;
    public bool RequiresSignature { get; set; }
    public string ContactPerson { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for linking purchase orders to orders
/// </summary>
public class OrderPurchaseOrderLinkDto
{
    public int OrderId { get; set; }
    public int PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public DateTime LinkedDate { get; set; }
    public string LinkedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}