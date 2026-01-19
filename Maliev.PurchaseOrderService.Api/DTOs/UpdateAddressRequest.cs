using Maliev.PurchaseOrderService.Common.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to update an address
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>
    /// Unique identifier for the address.
    /// </summary>
    public int? Id { get; set; }
    /// <summary>
    /// Type of address (Shipping or Billing).
    /// </summary>
    public AddressType? AddressType { get; set; }

    /// <summary>
    /// Company name (optional).
    /// </summary>
    [MaxLength(100)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Contact person name.
    /// </summary>
    [MaxLength(100)]
    public string? ContactName { get; set; }

    /// <summary>
    /// First address line.
    /// </summary>
    [MaxLength(100)]
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Second address line (optional).
    /// </summary>
    [MaxLength(100)]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City.
    /// </summary>
    [MaxLength(50)]
    public string? City { get; set; }

    /// <summary>
    /// State or province (optional).
    /// </summary>
    [MaxLength(50)]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Postal code.
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country.
    /// </summary>
    [MaxLength(50)]
    public string? Country { get; set; }

    /// <summary>
    /// Phone number (optional).
    /// </summary>
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email address (optional).
    /// </summary>
    [MaxLength(100)]
    [EmailAddress]
    public string? EmailAddress { get; set; }
}
