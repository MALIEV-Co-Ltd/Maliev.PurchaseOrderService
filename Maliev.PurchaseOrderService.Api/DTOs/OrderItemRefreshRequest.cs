using System.ComponentModel.DataAnnotations;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for refreshing order items from external order service
/// </summary>
public class OrderItemRefreshRequest
{
    /// <summary>
    /// List of order item IDs to refresh from external service
    /// </summary>
    [Required]
    public IEnumerable<int> OrderItemIds { get; set; } = new List<int>();

    /// <summary>
    /// Force refresh even if cached data is recent
    /// </summary>
    public bool ForceRefresh { get; set; } = false;

    /// <summary>
    /// Maximum age of cached data to consider valid (in minutes)
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Cache age must be between 1 and 1440 minutes")]
    public int? MaxCacheAgeMinutes { get; set; }
}