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

/// <summary>
/// Data Transfer Object for Supplier contact information
/// </summary>
public class SupplierContactDto
{
    public int SupplierId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string AlternateEmail { get; set; } = string.Empty;
    public string AlternatePhone { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for Supplier product catalog
/// </summary>
public class SupplierProductDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int MinOrderQuantity { get; set; }
    public int LeadTimeDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Data Transfer Object for Supplier payment terms
/// </summary>
public class SupplierPaymentTermsDto
{
    public int SupplierId { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public int PaymentDueDays { get; set; }
    public decimal EarlyPaymentDiscountPercent { get; set; }
    public int EarlyPaymentDiscountDays { get; set; }
    public decimal LateFeePercent { get; set; }
    public string PreferredPaymentMethod { get; set; } = string.Empty;
    public bool RequiresPurchaseOrder { get; set; }
    public decimal CreditLimit { get; set; }
    public string Currency { get; set; } = string.Empty;
}