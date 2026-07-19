using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Domain.Entities;

public class Address
{
    public int Id { get; set; }
    public AddressType AddressType { get; set; }
    public string? CompanyName { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
}
