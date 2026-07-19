namespace Maliev.PurchaseOrderService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Purchase Order Service.
/// </summary>
public static class PurchaseOrderPermissions
{
    public const string OrderCreate = "purchase-order.orders.create";
    public const string OrderRead = "purchase-order.orders.read";
    public const string OrderUpdate = "purchase-order.orders.update";
    public const string OrderDelete = "purchase-order.orders.delete";
    public const string OrderApprove = "purchase-order.orders.approve";
    public const string OrderCancel = "purchase-order.orders.cancel";

    public const string LineItemCreate = "purchase-order.line-items.create";
    public const string LineItemRead = "purchase-order.line-items.read";
    public const string LineItemUpdate = "purchase-order.line-items.update";
    public const string LineItemDelete = "purchase-order.line-items.delete";

    public const string ReportRead = "purchase-order.reports.read";
    public const string ReportExport = "purchase-order.reports.export";

    public const string SupplierManage = "purchase-order.suppliers.manage";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { OrderCreate, "Create purchase orders" },
        { OrderRead, "Read purchase orders" },
        { OrderUpdate, "Update purchase orders" },
        { OrderDelete, "Delete purchase orders" },
        { OrderApprove, "Approve purchase orders" },
        { OrderCancel, "Cancel purchase orders" },
        { LineItemCreate, "Create purchase order line items" },
        { LineItemRead, "Read purchase order line items" },
        { LineItemUpdate, "Update purchase order line items" },
        { LineItemDelete, "Delete purchase order line items" },
        { ReportRead, "Read purchase order reports" },
        { ReportExport, "Export purchase order reports" },
        { SupplierManage, "Manage purchase order suppliers" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
