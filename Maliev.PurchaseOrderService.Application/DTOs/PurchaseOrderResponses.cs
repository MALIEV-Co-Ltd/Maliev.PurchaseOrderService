using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Application.DTOs;

public record PurchaseOrderResponse
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string OrderType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int SupplierID { get; init; }
    public string SupplierName { get; init; } = string.Empty;
    public int OrderID { get; init; }
    public string CurrencyCode { get; init; } = "THB";
    public decimal TotalAmount { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record PurchaseOrderDetailResponse : PurchaseOrderResponse
{
    public string? SupplierContactInfo { get; init; }
    public string? CustomerPO { get; init; }
    public int CurrencyID { get; init; }
    public string? CurrencySymbol { get; init; }
    public DateTime? OrderDate { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal? WHTRate { get; init; }
    public decimal? WHTAmount { get; init; }
    public AddressResponse? ShippingAddress { get; init; }
    public AddressResponse? BillingAddress { get; init; }
    public string? Notes { get; init; }
    public List<OrderItemResponse> Items { get; init; } = new();
    public List<PurchaseOrderFileResponse> Files { get; init; } = new();
    public string? CreatedBy { get; init; }
    public string? LastModifiedBy { get; init; }
    public DateTime? LastModifiedAt { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public record OrderItemResponse
{
    public int Id { get; init; }
    public int ExternalOrderItemId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string UnitOfMeasure { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime CachedAt { get; init; }
    public bool ExternallyModified { get; init; }
}

public record AddressResponse
{
    public int Id { get; init; }
    public AddressType AddressType { get; init; }
    public string? CompanyName { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string? StateProvince { get; init; }
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
}

public record PurchaseOrderFileResponse
{
    public int Id { get; init; }
    public int PurchaseOrderId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ObjectName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DocumentType DocumentType { get; init; }
    public DateTime UploadedAt { get; init; }
    public string UploadedBy { get; init; } = string.Empty;
    public string? Description { get; init; }
}
