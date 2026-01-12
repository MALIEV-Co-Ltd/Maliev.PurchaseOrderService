namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Defines granular permission constants for the Purchase Order Service.
/// Follows GCP-style naming: {service}.{resource}.{action}
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
        /// <summary>Permission to send purchase orders to suppliers.</summary>
        public const string Send = "purchase-order.orders.send";
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
    /// Collection of all defined purchase order permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { Orders.Create, "Create purchase orders" },
        { Orders.Read, "Read purchase orders" },
        { Orders.Update, "Update purchase orders" },
        { Orders.Delete, "Delete purchase orders" },
        { Orders.Approve, "Approve purchase orders" },
        { Orders.Send, "Send purchase orders to suppliers" },
        { Orders.Cancel, "Cancel purchase orders" },
        { Orders.Receive, "Receive items against purchase orders" },
        { Orders.Export, "Export purchase orders" },
        { Suppliers.View, "View supplier information" },
        { Suppliers.Select, "Select suppliers for orders" },
        { Budgets.Check, "Perform budget checks" }
    };

    /// <summary>
    /// Gets all defined permission codes.
    /// </summary>
    public static string[] All => AllWithDescriptions.Keys.ToArray();
}

/// <summary>
/// Provides access to predefined roles for the Purchase Order Service.
/// </summary>
public static class PurchaseOrderPredefinedRoles
{
    /// <summary>Role for purchase order managers.</summary>
    public const string Manager = "roles.purchase-order.manager";
    /// <summary>Role for employees with basic access.</summary>
    public const string Employee = "roles.purchase-order.employee";

    /// <summary>
    /// Collection of all predefined roles for the Purchase Order Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Manager, "Full administrative access to purchase orders", PurchaseOrderPermissions.All),
        (Employee, "Basic purchase order creation and tracking", new[]
        {
            PurchaseOrderPermissions.Orders.Read,
            PurchaseOrderPermissions.Orders.Create,
            PurchaseOrderPermissions.Suppliers.View
        })
    };
}
