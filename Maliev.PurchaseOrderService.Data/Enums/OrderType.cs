namespace Maliev.PurchaseOrderService.Data.Enums;

/// <summary>
/// Type of purchase order based on purpose
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Purchase for company operations
    /// </summary>
    Internal = 0,

    /// <summary>
    /// Purchase for client projects
    /// </summary>
    External = 1
}