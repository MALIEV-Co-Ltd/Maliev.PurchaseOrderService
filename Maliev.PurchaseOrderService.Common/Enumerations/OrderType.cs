namespace Maliev.PurchaseOrderService.Common.Enumerations;

/// <summary>
/// Represents the type of purchase order
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Purchase for company operations (generates PDF automatically)
    /// </summary>
    Internal = 0,

    /// <summary>
    /// Purchase for client projects (customer-sent, no PDF generation)
    /// </summary>
    External = 1
}
