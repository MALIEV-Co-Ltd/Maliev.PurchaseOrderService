namespace Maliev.PurchaseOrderService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Purchase Order Service.
/// </summary>
public static class PurchaseOrderPredefinedRoles
{
    public const string Admin = "roles.purchase-order.admin";
    public const string Procurement = "roles.purchase-order.procurement";
    public const string Viewer = "roles.purchase-order.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Purchase Order Administrator with full access",
            new[]
            {
                PurchaseOrderPermissions.OrderCreate,
                PurchaseOrderPermissions.OrderRead,
                PurchaseOrderPermissions.OrderUpdate,
                PurchaseOrderPermissions.OrderDelete,
                PurchaseOrderPermissions.OrderApprove,
                PurchaseOrderPermissions.OrderCancel,
                PurchaseOrderPermissions.LineItemCreate,
                PurchaseOrderPermissions.LineItemRead,
                PurchaseOrderPermissions.LineItemUpdate,
                PurchaseOrderPermissions.LineItemDelete,
                PurchaseOrderPermissions.ReportRead,
                PurchaseOrderPermissions.ReportExport,
                PurchaseOrderPermissions.SupplierManage,
            }
        ),
        (
            Procurement,
            "Purchase Order Procurement role with create and approve access",
            new[]
            {
                PurchaseOrderPermissions.OrderCreate,
                PurchaseOrderPermissions.OrderRead,
                PurchaseOrderPermissions.OrderUpdate,
                PurchaseOrderPermissions.OrderApprove,
                PurchaseOrderPermissions.LineItemCreate,
                PurchaseOrderPermissions.LineItemRead,
                PurchaseOrderPermissions.LineItemUpdate,
                PurchaseOrderPermissions.ReportRead,
                PurchaseOrderPermissions.SupplierManage,
            }
        ),
        (
            Viewer,
            "Purchase Order Viewer with read-only access",
            new[]
            {
                PurchaseOrderPermissions.OrderRead,
                PurchaseOrderPermissions.LineItemRead,
                PurchaseOrderPermissions.ReportRead,
            }
        ),
    };
}
