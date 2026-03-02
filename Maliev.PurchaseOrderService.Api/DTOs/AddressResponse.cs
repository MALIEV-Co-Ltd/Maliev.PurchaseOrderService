using Maliev.PurchaseOrderService.Domain.Enumerations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Address response
/// </summary>
public class AddressResponse
{
    /// <summary>
    /// Unique identifier for the address.
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// Type of address (Shipping or Billing).
    /// </summary>
    public AddressType AddressType { get; set; }
    /// <summary>
    /// Name of the company associated with the address.
    /// </summary>
    public string? CompanyName { get; set; }
    /// <summary>
    /// Name of the contact person.
    /// </summary>
    public string ContactName { get; set; } = string.Empty;
    /// <summary>
    /// First line of the address.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;
    /// <summary>
    /// Second line of the address (optional).
    /// </summary>
    public string? AddressLine2 { get; set; }
    /// <summary>
    /// City name.
    /// </summary>
    public string City { get; set; } = string.Empty;
    /// <summary>
    /// State or province name.
    /// </summary>
    public string? StateProvince { get; set; }
    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>
    /// Country name.
    /// </summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? EmailAddress { get; set; }
}
