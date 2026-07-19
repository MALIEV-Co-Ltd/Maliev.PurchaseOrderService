# Quickstart: IAM Integration

## Prerequisites
- Access to MALIEV IAM Service (or local container).
- `Maliev.Aspire.ServiceDefaults` updated.

## Setup
1. Define permissions in `PurchaseOrderPermissions.cs`.
2. Map permissions to roles in `PurchaseOrderPredefinedRoles.cs`.
3. Register the `IUserPermissionService` and `PurchaseOrderIAMRegistrationService` in `Program.cs`.

## Implementation Details
### Permission Enforcement
Use the local `RequirePurchaseOrderPermission` attribute on controller actions. This attribute:
- Checks for the specific permission in the user's JWT `permissions` claim.
- Supports wildcards (e.g., `purchase-order.*`, `purchase-order.orders.*`).
- **Automatically logs all authorization decisions (grants and denials) to the `IAuditLogService`.**

Example:
```csharp
[HttpPost]
[RequirePurchaseOrderPermission(PurchaseOrderPermissions.Orders.Create)]
public async Task<ActionResult> CreatePurchaseOrder(...)
```

### Automatic Registration
The `PurchaseOrderIAMRegistrationService` automatically registers all 12 permissions and 6 predefined roles with the IAM service during application startup.

## Testing Permissions
Use `IAMTestHelpers` in integration tests:

```csharp
[Fact]
public async Task CreateOrder_WithCreatePermission_ReturnsCreated()
{
    // Arrange
    var client = Factory.CreateClient()
        .WithTestAuth(PurchaseOrderPermissions.Orders.Create);

    // Act
    var response = await client.PostAsJsonAsync("/purchase-orders", newRequest);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

## Troubleshooting
- **403 Forbidden**: Check if the user's JWT has the `permissions` claim. Verify if the permission matches the expected format `purchase-order.[resource].[action]`.
- **Registration Failed**: Check `IAMService` connectivity and logs. The service expects an `IAMService:BaseUrl` configuration.