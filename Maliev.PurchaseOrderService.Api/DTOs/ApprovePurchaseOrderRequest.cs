namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to approve a purchase order
/// </summary>
public class ApprovePurchaseOrderRequest
{
    /// <summary>
    /// User approving the purchase order
    /// </summary>
    public string ApprovedBy { get; set; } = string.Empty;

    /// <summary>
    /// Approval comments
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Approval level (e.g., Manager, Director, etc.)
    /// </summary>
    public string? ApprovalLevel { get; set; }

    /// <summary>
    /// Approval timestamp (optional, defaults to current time)
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Whether to generate PDF after approval (for internal POs)
    /// </summary>
    public bool GeneratePdf { get; set; } = true;

    /// <summary>
    /// Whether to send notifications after approval
    /// </summary>
    public bool SendNotifications { get; set; } = true;

    /// <summary>
    /// Additional metadata for approval
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}