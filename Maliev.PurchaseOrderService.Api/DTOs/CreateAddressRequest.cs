using Maliev.PurchaseOrderService.Common.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to create a new address
/// </summary>
public class CreateAddressRequest
{
    /// <summary>
    /// Type of address (Shipping or Billing)
    /// </summary>
    [Required]
    public AddressType AddressType { get; set; }

    /// <summary>
    /// Company name (optional)
    /// </summary>
    [MaxLength(100)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Contact person name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// First address line
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Second address line (optional)
    /// </summary>
    [MaxLength(100)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province (optional)
    /// </summary>
    [MaxLength(50)]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Phone number (optional)
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email address (optional)
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? EmailAddress { get; set; }
}
