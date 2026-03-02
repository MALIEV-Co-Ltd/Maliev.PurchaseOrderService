using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Application.DTOs;

public record CreatePurchaseOrderRequest
{
    public OrderType OrderType { get; init; }
    public int SupplierID { get; init; }
    public int OrderID { get; init; }
    public string? CustomerPO { get; init; }
    public int CurrencyID { get; init; }
    public decimal WHTRate { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public string? Notes { get; init; }
    public List<CreateOrderItemRequest> Items { get; init; } = new();
    public CreateAddressRequest? ShippingAddress { get; init; }
    public CreateAddressRequest? BillingAddress { get; init; }
}

public record CreateOrderItemRequest(
    string ProductCode,
    string ProductName,
    decimal Quantity,
    string UnitOfMeasure,
    decimal UnitPrice,
    string Currency,
    string? Notes
);

public record CreateAddressRequest(
    AddressType AddressType,
    string CompanyName,
    string ContactName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? StateProvince,
    string PostalCode,
    string Country,
    string? PhoneNumber,
    string? EmailAddress
);

public record UpdatePurchaseOrderRequest
{
    public string? Notes { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public List<CreateOrderItemRequest>? Items { get; init; }
}

public record UpdateAddressRequest(
    AddressType? AddressType,
    string CompanyName,
    string? ContactName,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    string? PhoneNumber,
    string? EmailAddress
);

public record SearchPurchaseOrdersRequest(
    int? SupplierId,
    string? Status,
    int? OrderType,
    int? OrderID,
    DateTime? FromDate,
    DateTime? ToDate,
    int Page = 1,
    int PageSize = 20
);

public record PaginatedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public record CancelPurchaseOrderRequest(string Reason);
