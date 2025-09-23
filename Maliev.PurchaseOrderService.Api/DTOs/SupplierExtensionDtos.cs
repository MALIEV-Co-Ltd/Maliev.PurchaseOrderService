namespace Maliev.PurchaseOrderService.Api.DTOs;

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

/// <summary>
/// Data Transfer Object for Supplier Contact Information
/// </summary>
public class SupplierContactDto
{
    /// <summary>
    /// Supplier ID
    /// </summary>
    public int SupplierId { get; set; }

    /// <summary>
    /// Contact name
    /// </summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Contact title/position
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Contact title (alias for compatibility)
    /// </summary>
    public string ContactTitle { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact email (alias for compatibility)
    /// </summary>
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone (alias for compatibility)
    /// </summary>
    public string ContactPhone { get; set; } = string.Empty;

    /// <summary>
    /// Mobile phone number
    /// </summary>
    public string? Mobile { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Whether this is the primary contact
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Whether this contact is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last contacted date
    /// </summary>
    public DateTime? LastContactedAt { get; set; }
}