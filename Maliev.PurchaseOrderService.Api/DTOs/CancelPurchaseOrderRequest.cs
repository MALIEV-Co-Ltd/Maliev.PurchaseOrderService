using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to cancel a purchase order
/// </summary>
public class CancelPurchaseOrderRequest
{
    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
