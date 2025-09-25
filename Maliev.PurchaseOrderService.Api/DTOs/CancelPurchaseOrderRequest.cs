using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to cancel a purchase order
/// </summary>
public class CancelPurchaseOrderRequest
{
    /// <summary>
    /// User canceling the purchase order
    /// </summary>
    public string CanceledBy { get; set; } = string.Empty;

    /// <summary>
    /// User roles for authorization (populated from claims)
    /// </summary>
    public List<string>? UserRoles { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    public string? RowVersion { get; set; }

    /// <summary>
    /// Reason for cancellation
    /// </summary>
    [Required(ErrorMessage = "Reason is required")]
    [MinLength(1, ErrorMessage = "Reason cannot be empty")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Additional comments about the cancellation
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Cancellation timestamp (optional, defaults to current time)
    /// </summary>
    public DateTime? CanceledAt { get; set; }

    /// <summary>
    /// Whether to send notifications after cancellation
    /// </summary>
    public bool SendNotifications { get; set; } = true;

    /// <summary>
    /// Whether to automatically handle related documents
    /// </summary>
    public bool ArchiveDocuments { get; set; } = true;

    /// <summary>
    /// Additional metadata for cancellation
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}