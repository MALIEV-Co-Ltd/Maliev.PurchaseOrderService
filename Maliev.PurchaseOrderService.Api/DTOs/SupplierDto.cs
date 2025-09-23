namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Data Transfer Object for Supplier information
/// </summary>
public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public string? ContactInfo { get; set; }
    public string SupplierType { get; set; } = "company";
    public string ServiceCategory { get; set; } = "services";
    public bool IsThaiResident { get; set; } = true;
    public bool IsWHTExempt { get; set; } = false;
    public AddressDto? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}