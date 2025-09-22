using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Address data transfer object for API operations
/// Shipping and billing addresses for purchase orders
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Shipping or Billing
    /// </summary>
    [Required]
    public AddressType AddressType { get; set; }

    /// <summary>
    /// Company name
    /// </summary>
    [StringLength(100)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Contact person name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContactName { get; set; } = string.Empty;

    /// <summary>
    /// Primary address line
    /// </summary>
    [Required]
    [StringLength(100)]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary address line
    /// </summary>
    [StringLength(100)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    [StringLength(50)]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

