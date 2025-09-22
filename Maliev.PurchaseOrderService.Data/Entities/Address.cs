using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Data.Entities;

/// <summary>
/// Shipping and billing addresses for purchase orders
/// </summary>
[Table("Addresses")]
public class Address
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    [Key]
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
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Flag indicating if this address is active/valid
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Flag indicating if this address has been validated
    /// </summary>
    [Required]
    public bool IsValidated { get; set; } = false;

    /// <summary>
    /// Date when address validation was performed
    /// </summary>
    public DateTime? ValidatedAt { get; set; }

    /// <summary>
    /// User who created this address
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who last modified this address
    /// </summary>
    [StringLength(50)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Optimistic concurrency control token
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    // Navigation Properties

    /// <summary>
    /// Purchase orders using this as shipping address
    /// </summary>
    public virtual ICollection<PurchaseOrder> ShippingPurchaseOrders { get; set; } = new List<PurchaseOrder>();

    /// <summary>
    /// Purchase orders using this as billing address
    /// </summary>
    public virtual ICollection<PurchaseOrder> BillingPurchaseOrders { get; set; } = new List<PurchaseOrder>();
}