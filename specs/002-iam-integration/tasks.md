# Tasks: IAM Integration

**Input**: Design documents from `specs/002-iam-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md

**Tests**: This task list includes integration tests as requested in Phase 4 of the implementation plan and required by Constitution III.

**Organization**: Tasks are grouped by foundational infrastructure followed by prioritized user stories to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Definitions)

**Purpose**: Define the constants for permissions and roles that will be used throughout the service.

- [x] T001 [P] Create permission constants in `Maliev.PurchaseOrderService.Api/Authorization/PurchaseOrderPermissions.cs`
- [x] T002 [P] Create predefined role mappings in `Maliev.PurchaseOrderService.Api/Authorization/PurchaseOrderPredefinedRoles.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure for IAM registration, caching, and auditing.

### Foundational Tests (REQUIRED - Constitution III)

- [x] T003 [P] Create unit tests for `UserPermissionService` caching logic in `Maliev.PurchaseOrderService.Tests/Unit/UserPermissionServiceTests.cs`
- [x] T004 [P] Create integration tests for `PurchaseOrderIAMRegistrationService` using Testcontainers in `Maliev.PurchaseOrderService.Tests/Integration/IAMRegistrationTests.cs`

### Foundational Implementation

- [x] T005 Create `IUserPermissionService` interface and implementation with 10-minute MemoryCache (TTL) in `Maliev.PurchaseOrderService.Api/Services/UserPermissionService.cs`
- [x] T006 Implement `PurchaseOrderIAMRegistrationService` inheriting from `IAMRegistrationService` in `Maliev.PurchaseOrderService.Api/Authorization/PurchaseOrderIAMRegistrationService.cs`
- [x] T007 Register `UserPermissionService` and `PurchaseOrderIAMRegistrationService` (as HostedService) in `Maliev.PurchaseOrderService.Api/Program.cs`
- [x] T008 Configure "IAMService" HttpClient with base URL and resilience in `Maliev.PurchaseOrderService.Api/Program.cs`
- [x] T009 Configure `MemoryCache` and Redis distributed cache if not already present in `Maliev.PurchaseOrderService.Api/Program.cs`

**Checkpoint**: Foundation ready - permission registration and checking logic is in place and verified.

---

## Phase 3: User Story 1 & 2 - Core Procurement & Admin (Priority: P1) 🎯 MVP

**Goal**: Enable administrators and procurement officers to perform core PO operations with fine-grained permissions.

### Tests for US1 & US2

- [x] T010 [P] [US1/2] Create integration tests (using Testcontainers) for PO creation permission in `Maliev.PurchaseOrderService.Tests/Integration/PurchaseOrdersControllerPermissionTests.cs`
- [x] T011 [P] [US1/2] Create integration tests for Admin delete permission in `Maliev.PurchaseOrderService.Tests/Integration/PurchaseOrdersControllerPermissionTests.cs`
- [x] T012 [P] [US1/2] Create integration test for Hierarchical permissions (e.g., `purchase-order.*`) in `Maliev.PurchaseOrderService.Tests/Integration/PurchaseOrdersControllerPermissionTests.cs`

### Implementation for US1 & US2

- [x] T013 [US1/2] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Create)]` to `CreatePurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T014 [US1/2] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Read)]` to `GetPurchaseOrders` and `GetPurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T015 [US1/2] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Update)]` to `UpdatePurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T016 [US1/2] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Delete)]` to `DeletePurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T017 [US1/2] Apply `[RequirePermission(PurchaseOrderPermissions.Suppliers.View)]` and `[RequirePermission(PurchaseOrderPermissions.Suppliers.Select)]` to relevant actions in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T018 [US1/2] Integrate `IAuditLogService` into the permission check flow to log all grants and denials.
- [x] T019 [US1/2] Verify resource-level ownership checks (IDOR) still function correctly alongside new permissions in `PurchaseOrderService`.

---

## Phase 4: User Story 3 - Order Approval Flow (Priority: P2)

**Goal**: Protect approval and cancellation actions with specific permissions.

### Tests for US3

- [x] T020 [P] [US3] Create integration tests for Approve/Cancel permissions in `Maliev.PurchaseOrderService.Tests/Integration/PurchaseOrdersControllerPermissionTests.cs`

### Implementation for US3

- [x] T021 [US3] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Approve)]` to `ApprovePurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T022 [US3] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Cancel)]` to `CancelPurchaseOrder` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`
- [x] T023 [US3] Apply `[RequirePermission(PurchaseOrderPermissions.Budgets.Check)]` to relevant actions in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`

---

## Phase 5: User Story 4 - Receiving and Verification (Priority: P2)

**Goal**: Protect the receiving process with `orders.receive` permission.

### Tests for US4

- [x] T024 [P] [US4] Create integration tests for Receive permission in `Maliev.PurchaseOrderService.Tests/Integration/PurchaseOrdersControllerPermissionTests.cs`

### Implementation for US4

- [x] T025 [US4] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Receive)]` to `ReceiveItems` in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`

---

## Phase 6: User Story 5 - Read-Only Auditing (Priority: P3)

**Goal**: Ensure Viewers have access to all read-only endpoints but no write endpoints.

### Implementation for US5

- [x] T026 [US5] Verify all GET endpoints in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs` have `orders.read` or `suppliers.view`.
- [x] T027 [US5] Apply `[RequirePermission(PurchaseOrderPermissions.Orders.Export)]` to the export action in `Maliev.PurchaseOrderService.Api/Controllers/PurchaseOrdersController.cs`

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Cleanup and final validation.

- [x] T028 Remove old role-based authorization policies from `Maliev.PurchaseOrderService.Api/Program.cs`
- [x] T029 [P] Update `specs/002-iam-integration/quickstart.md` with final implementation details
- [x] T030 Run all integration tests and verify 100% pass rate and average auth latency < 5ms
- [x] T031 Verify zero compiler warnings in all projects

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies.
- **Phase 2 (Foundational)**: Depends on Phase 1.
- **Phase 3-6 (User Stories)**: All depend on Phase 2. Can be worked on in parallel.
- **Phase 7 (Polish)**: Depends on completion of all migration tasks.

### Parallel Opportunities

- T001 and T002 can be done in parallel.
- Integration tests can be developed in parallel with implementation tasks within their respective phases.
- Once Phase 2 is complete, US1/2, US3, and US4 can be implemented in parallel.

---

## Implementation Strategy

### MVP First (Core Procurement)
Focus on Phase 1, 2, and 3. This provides the core functionality with the new permission system and verifies all foundational requirements.

### Incremental Delivery
Each phase adds a distinct set of permissions and roles, allowing for gradual migration. Verify each US phase independently before moving to the next.
