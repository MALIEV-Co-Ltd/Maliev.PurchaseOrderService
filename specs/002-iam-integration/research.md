# Research: IAM Integration for PurchaseOrderService

## Decision: Use `Maliev.Aspire.ServiceDefaults` for IAM Integration
The project already has a shared infrastructure for IAM in `Maliev.Aspire.ServiceDefaults`. This includes:
- `RequirePermissionAttribute` for declarative authorization.
- `IAMRegistrationService` for automated permission/role registration.
- `IAMTestHelpers` for integration testing with permissions.

## Rationale
Using the shared library ensures consistency across MALIEV microservices and reduces implementation effort. It already supports:
- Wildcard matching (`service.*`, `service.resource.*`).
- Standard permission format: `service.resource.action`.
- Automated registration on startup.

## Caching Strategy
The spec requires a "short-lived local cache". Since `RequirePermissionAttribute` currently checks claims in the JWT, I will:
1. Continue to use JWT claims for primary checks.
2. Implement a `UserPermissionService` that can optionally fetch the latest permissions from IAM and cache them using `IMemoryCache` with a 10-minute TTL, overriding JWT claims if necessary for near real-time revocation.
3. Update `RequirePermissionAttribute` to use this service.

## Audit Logging
I will extend the `RequirePermissionAttribute` or implement a custom `IPermissionService` that calls `IAuditLogService.CreateAuditLogAsync` for every authorization decision.

## Implementation Details
- **Namespace**: `Maliev.PurchaseOrderService.Api.Authorization`
- **Registration Class**: `PurchaseOrderIAMRegistrationService` inheriting from `IAMRegistrationService`.
- **Permissions Class**: `PurchaseOrderPermissions` with `public const string` values.
- **Roles Class**: `PurchaseOrderPredefinedRoles` for mapping.
