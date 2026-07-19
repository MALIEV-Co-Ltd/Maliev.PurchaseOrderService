# Data Model: Permissions and Roles

## Permissions
Permissions follow the format: `purchase-order.[resource].[action]`

| Permission ID | Description |
|---------------|-------------|
| `purchase-order.orders.create` | Create new purchase orders |
| `purchase-order.orders.read` | Read purchase order details |
| `purchase-order.orders.update` | Update purchase order information |
| `purchase-order.orders.delete` | Delete purchase orders |
| `purchase-order.orders.approve` | Approve purchase orders |
| `purchase-order.orders.cancel` | Cancel purchase orders |
| `purchase-order.orders.receive` | Mark goods as received |
| `purchase-order.orders.export` | Export purchase orders |
| `purchase-order.suppliers.view` | View supplier information |
| `purchase-order.suppliers.select` | Select suppliers for orders |
| `purchase-order.budgets.check` | Check budget availability |
| `purchase-order.budgets.allocate` | Allocate budget to orders |

## Predefined Roles

### `purchase-order-admin`
- All `purchase-order.*` permissions.

### `purchase-order-manager`
- `orders.create`, `orders.read`, `orders.update`, `orders.approve`, `orders.cancel`
- `suppliers.view`
- `budgets.check`

### `purchase-order-procurement`
- `orders.create`, `orders.read`, `orders.update`
- `suppliers.view`, `suppliers.select`
- `budgets.check`

### `purchase-order-approver`
- `orders.read`, `orders.approve`, `orders.cancel`
- `budgets.check`

### `purchase-order-receiver`
- `orders.read`, `orders.receive`

### `purchase-order-viewer`
- `orders.read`
