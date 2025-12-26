namespace Maliev.PurchaseOrderService.Api.Services;

public static class PurchaseOrderPermissions
{
    public static class Orders
    {
        public const string Create = "purchase-order.orders.create";
        public const string Read = "purchase-order.orders.read";
        public const string Update = "purchase-order.orders.update";
        public const string Delete = "purchase-order.orders.delete";
        public const string Approve = "purchase-order.orders.approve";
        public const string Cancel = "purchase-order.orders.cancel";
        public const string Receive = "purchase-order.orders.receive";
        public const string Export = "purchase-order.orders.export";
    }

    public static class Suppliers
    {
        public const string View = "purchase-order.suppliers.view";
        public const string Select = "purchase-order.suppliers.select";
    }

    public static class Budgets
    {
        public const string Check = "purchase-order.budgets.check";
    }

    public static string[] All => new[]
    {
        Orders.Create, Orders.Read, Orders.Update, Orders.Delete,
        Orders.Approve, Orders.Cancel, Orders.Receive, Orders.Export,
        Suppliers.View, Suppliers.Select,
        Budgets.Check
    };
}

public class PredefinedRole
{
    public string RoleId { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string[] Permissions { get; init; } = default!;
}

public static class PurchaseOrderPredefinedRoles
{
    public static readonly PredefinedRole Manager = new()
    {
        RoleId = "purchase-order.manager",
        Description = "Purchase Order Manager",
        Permissions = PurchaseOrderPermissions.All
    };

    public static readonly PredefinedRole Employee = new()
    {
        RoleId = "purchase-order.employee",
        Description = "Purchase Order Employee",
        Permissions = new[] {
            PurchaseOrderPermissions.Orders.Read,
            PurchaseOrderPermissions.Orders.Create,
            PurchaseOrderPermissions.Suppliers.View
        }
    };

    public static string[] AllRoleIds => new[] { Manager.RoleId, Employee.RoleId };
    public static PredefinedRole[] All => new[] { Manager, Employee };
}
