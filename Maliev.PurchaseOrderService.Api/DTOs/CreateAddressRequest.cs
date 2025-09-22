using System.ComponentModel.DataAnnotations;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

public class CreateAddressRequest
{
    [Required]
    public AddressType AddressType { get; set; }

    [MaxLength(100)]
    public string? CompanyName { get; set; }

    [Required]
    [MaxLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AddressLine2 { get; set; }

    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? StateProvince { get; set; }

    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Country { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? EmailAddress { get; set; }
}