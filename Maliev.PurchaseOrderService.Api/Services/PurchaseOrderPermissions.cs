namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Defines permission constants for the Purchase Order Service.
/// </summary>
public static class PurchaseOrderPermissions
{
    /// <summary>Permissions related to purchase orders.</summary>
    public static class Orders
    {
        /// <summary>Permission to create purchase orders.</summary>
        public const string Create = "purchase-order.orders.create";
        /// <summary>Permission to read purchase orders.</summary>
        public const string Read = "purchase-order.orders.read";
        /// <summary>Permission to update purchase orders.</summary>
        public const string Update = "purchase-order.orders.update";
        /// <summary>Permission to delete purchase orders.</summary>
        public const string Delete = "purchase-order.orders.delete";
        /// <summary>Permission to approve purchase orders.</summary>
        public const string Approve = "purchase-order.orders.approve";
        /// <summary>Permission to cancel purchase orders.</summary>
        public const string Cancel = "purchase-order.orders.cancel";
        /// <summary>Permission to receive items against purchase orders.</summary>
        public const string Receive = "purchase-order.orders.receive";
        /// <summary>Permission to export purchase orders.</summary>
        public const string Export = "purchase-order.orders.export";
    }

    /// <summary>Permissions related to suppliers.</summary>
    public static class Suppliers
    {
        /// <summary>Permission to view supplier information.</summary>
        public const string View = "purchase-order.suppliers.view";
        /// <summary>Permission to select suppliers for orders.</summary>
        public const string Select = "purchase-order.suppliers.select";
    }

    /// <summary>Permissions related to budget management.</summary>
    public static class Budgets
    {
        /// <summary>Permission to perform budget checks.</summary>
        public const string Check = "purchase-order.budgets.check";
    }

    /// <summary>
    /// Gets all defined permissions.
    /// </summary>
    public static string[] All => new[]
    {
        Orders.Create, Orders.Read, Orders.Update, Orders.Delete,
        Orders.Approve, Orders.Cancel, Orders.Receive, Orders.Export,
        Suppliers.View, Suppliers.Select,
        Budgets.Check
    };
}

/// <summary>
/// Represents a predefined role with associated permissions.
/// </summary>
public class PredefinedRole
{
    /// <summary>Gets the unique identifier for the role.</summary>
    public string RoleId { get; init; } = default!;
    /// <summary>Gets the description of the role.</summary>
    public string Description { get; init; } = default!;
    /// <summary>Gets the permissions assigned to the role.</summary>
    public string[] Permissions { get; init; } = default!;
}

/// <summary>
/// Provides access to predefined roles for the Purchase Order Service.
/// </summary>
public static class PurchaseOrderPredefinedRoles
{
    /// <summary>The manager role with all permissions.</summary>
    public static readonly PredefinedRole Manager = new()
    {
        RoleId = "purchase-order.manager",
        Description = "Purchase Order Manager",
        Permissions = PurchaseOrderPermissions.All
    };

    /// <summary>The employee role with basic permissions.</summary>
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

    /// <summary>Gets all predefined role IDs.</summary>
    public static string[] AllRoleIds => new[] { Manager.RoleId, Employee.RoleId };
    /// <summary>Gets all predefined roles.</summary>
    public static PredefinedRole[] All => new[] { Manager, Employee };
}