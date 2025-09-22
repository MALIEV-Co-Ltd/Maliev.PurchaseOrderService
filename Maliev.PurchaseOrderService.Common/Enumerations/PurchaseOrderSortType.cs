namespace Maliev.PurchaseOrderService.Common.Enumerations;

/// <summary>
/// Enumeration for purchase order sorting options
/// </summary>
public enum PurchaseOrderSortType
{
    /// <summary>
    /// Sort by purchase order number ascending
    /// </summary>
    OrderNumber,

    /// <summary>
    /// Sort by purchase order number descending
    /// </summary>
    OrderNumberDesc,

    /// <summary>
    /// Sort by total amount ascending
    /// </summary>
    TotalAmount,

    /// <summary>
    /// Sort by total amount descending
    /// </summary>
    TotalAmountDesc,

    /// <summary>
    /// Sort by creation date ascending
    /// </summary>
    CreatedAt,

    /// <summary>
    /// Sort by creation date descending
    /// </summary>
    CreatedAtDesc,

    /// <summary>
    /// Sort by supplier ID ascending
    /// </summary>
    SupplierId,

    /// <summary>
    /// Sort by supplier ID descending
    /// </summary>
    SupplierIdDesc,

    /// <summary>
    /// Sort by status ascending
    /// </summary>
    Status,

    /// <summary>
    /// Sort by status descending
    /// </summary>
    StatusDesc,

    /// <summary>
    /// Sort by approval date ascending
    /// </summary>
    ApprovedAt,

    /// <summary>
    /// Sort by approval date descending
    /// </summary>
    ApprovedAtDesc
}