# Implementation Plan: IAM Integration

**Feature Branch**: `002-iam-integration`  
**Status**: Planning  
**Linked Spec**: [spec.md](spec.md)

## Technical Context

- **Framework**: .NET 10
- **Shared Library**: `Maliev.Aspire.ServiceDefaults` (provides `RequirePermissionAttribute` and `IAMRegistrationService`)
- **Infrastructure**: Redis for distributed caching, PostgreSQL for data (existing), MassTransit for messaging (existing).
- **IAM Client**: Standard `HttpClient` using the "IAMService" configuration.
- **Authorization**: Fine-grained permission-based authorization using `[RequirePermission]` attribute.
- **Auditing**: Log all auth decisions to `IAuditLogService`.

## Constitution Check

- [x] Service Autonomy: Maintained; IAM integration follows platform standards.
- [x] Explicit Contracts: OpenAPI contract updated to reflect permission requirements.
- [x] Test-First: Phase 4 dedicated to comprehensive integration tests.
- [x] Real Infrastructure: Tests will use Testcontainers for PostgreSQL, Redis, and RabbitMQ.
- [x] Auditability: Authorization decisions logged.
- [x] Secrets Management: IAM credentials/base URLs in appsettings/Google Secret Manager.
- [x] NO AutoMapper/FluentValidation: Followed.
- [x] Docker Best Practices: Followed.

## Phase 0: Research & Foundation (Complete)
- [x] Identify IAM client library in `Maliev.Aspire.ServiceDefaults`.
- [x] Research `RequirePermissionAttribute` implementation.
- [x] Define caching and auditing strategy.

## Phase 1: Design & Contracts (Complete)
- [x] Generate `data-model.md` for permissions/roles.
- [x] Update OpenAPI contract in `contracts/purchase-orders-api.yaml`.
- [x] Generate `quickstart.md`.

## Phase 2: Core Implementation (Estimated: 4 hours)
1. **Define Permissions & Roles**:
   - Create `Maliev.PurchaseOrderService.Api.Authorization.PurchaseOrderPermissions`
   - Create `Maliev.PurchaseOrderService.Api.Authorization.PurchaseOrderPredefinedRoles`
2. **Registration Service**:
   - Create `PurchaseOrderIAMRegistrationService` inheriting from `IAMRegistrationService`.
   - Implement `GetPermissions()` and `GetRoles()`.
   - Register in `Program.cs` as a hosted service.
3. **Auditing & Caching Bridge**:
   - Implement `IPermissionService` to handle `IMemoryCache` (TTL) and call `IAuditLogService`.
   - (Optional) Update `RequirePermissionAttribute` in `ServiceDefaults` if needed, or implement a local variant if shared one is too restrictive.

## Phase 3: Controller Migration (Estimated: 3 hours)
1. **PurchaseOrdersController**: Update all actions with `[RequirePermission]`.
2. **Remove Role-Based Policies**: Update `Program.cs` and remove old policies if no longer used.

## Phase 4: Verification (Estimated: 4 hours)
1. **Update Integration Tests**: Use `IAMTestHelpers.WithTestAuth` to simulate permissions.
2. **Scenario Coverage**: Test all 6 predefined roles and edge cases (multiple roles, missing permissions).

## Phase 5: Deployment (Estimated: 2 hours)
1. **Verify Registration**: Ensure permissions/roles appear in IAM registry on dev/staging.
2. **Final Smoke Test**: Verify end-to-side flow.