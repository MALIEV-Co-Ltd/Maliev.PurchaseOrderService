# Implementation Tasks: PurchaseOrderService Microservice

**Feature**: Create PurchaseOrderService microservice with external service integration
**Date**: 2025-11-26
**Context**: .NET 10 microservice with 5 external service integrations, event-driven PDF generation, WHT calculations, and .NET Aspire ServiceDefaults integration via private NuGet feed.

## Task Overview

This document contains actionable, dependency-ordered tasks for implementing the PurchaseOrderService. The tasks are derived from the latest `plan.md` and `spec.md`, organized by user story to facilitate independent, parallel development and testing.

---

## Phase 1: Project Setup & Configuration

This phase establishes the foundational structure of the solution, including project files, configurations, and dependencies.

- [X] T001 Create solution `Maliev.PurchaseOrderService.sln` at the repository root.
- [X] T002 [P] Create the API project `Maliev.PurchaseOrderService.Api/Maliev.PurchaseOrderService.Api.csproj`.
- [X] T003 [P] Create the Data project `Maliev.PurchaseOrderService.Data/Maliev.PurchaseOrderService.Data.csproj`.
- [X] T004 [P] Create the Tests project `Maliev.PurchaseOrderService.Tests/Maliev.PurchaseOrderService.Tests.csproj`.
- [X] T004a [P] Create the Common project `Maliev.PurchaseOrderService.Common/Maliev.PurchaseOrderService.Common.csproj`.
- [X] T005 Set up project references: Api -> Data, Tests -> Api.
- [X] T006 Create `nuget.config` at the solution root with the provided XML configuration for `nuget.org` and the private `github` package source.
- [X] T007 [P] Add the package reference `<PackageReference Include="Maliev.Aspire.ServiceDefaults" Version="1.0.*" />` to `Maliev.PurchaseOrderService.Api.csproj`.
- [X] T008 [P] Add core NuGet packages to `Maliev.PurchaseOrderService.Api`: `Serilog.AspNetCore`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore`.
- [X] T009 [P] Add EF Core packages to `Maliev.PurchaseOrderService.Data`: `Npgsql.EntityFrameworkCore.PostgreSQL`.
- [X] T010 [P] Add testing packages to `Maliev.PurchaseOrderService.Tests`: `xUnit`, `Moq`, `FluentAssertions`, `Testcontainers`, `Microsoft.AspNetCore.Mvc.Testing`.
- [X] T011 Create the multi-stage `Dockerfile` in the solution root. Adapt it from the `AuthService` example, ensuring it copies `nuget.config`, uses BuildKit secrets for `dotnet restore`, and uses the existing non-root 'app' user from the base image.

---

## Phase 2: Foundational Layer

This phase implements core components and configurations that are prerequisites for all user stories.

- [X] T012 In `Maliev.PurchaseOrderService.Api/Program.cs`, add `builder.AddServiceDefaults();` after the WebApplication builder is created.
- [X] T013 [P] In `Maliev.PurchaseOrderService.Api/Program.cs`, configure Serilog for structured JSON logging to stdout, reading configuration from `appsettings.json`.
- [X] T014 [P] Define the base entities in `Maliev.PurchaseOrderService.Data/Entities/`: `PurchaseOrder.cs`, `OrderItem.cs`, `Address.cs`, `PurchaseOrderFile.cs`, `AuditLog.cs`, `DomainEvent.cs`.
- [X] T015 [P] Create `Maliev.PurchaseOrderService.Common` class library project, define shared enumerations (e.g., `OrderStatus`, `POType`) within it, and add references to it in Data and Api projects.
- [X] T016 Create the `PurchaseOrderContext.cs` DbContext in `Maliev.PurchaseOrderService.Data/`, inheriting from `DbContext`.
- [X] T017 Configure entity relationships (e.g., using `OnModelCreating`) and constraints within `PurchaseOrderContext.cs`.
- [X] T018 Generate the initial EF Core migration for the database schema using `dotnet ef migrations add InitialCreate`.
- [X] T019 Configure JWT Bearer authentication and authorization policies in `Maliev.PurchaseOrderService.Api/Program.cs`.
- [X] T020 Configure CORS in `Maliev.PurchaseOrderService.Api/Program.cs` based on environment-specific origins from the configuration.
- [X] T021 [P] Set up `HttpClientFactory` in `Program.cs` for external service clients and add resilience policies using `Microsoft.Extensions.Http.Resilience`.
- [X] T021a [P] Configure in-memory caching (or distributed cache if available) with a 1-hour TTL for `CurrencyService` data to satisfy NFR-001.
- [X] T022 In `Maliev.PurchaseOrderService.Api/Program.cs`, configure the Scalar and OpenAPI endpoints for Development and Staging environments, adapting paths for the Purchase Order service (e.g., `/purchase-orders/scalar/v1`, `/purchase-orders/openapi/{documentName}.json`).

---

## Phase 3: [US1] Create and Manage Purchase Orders

**User Story**: As a procurement team member, I need to create and manage purchase orders based on customer orders/quotations.

- [X] T023 [P] [US1] Create DTOs for purchase order creation (`CreatePurchaseOrderRequest.cs`) and response (`PurchaseOrderDto.cs`) in `Maliev.PurchaseOrderService.Api/DTOs/`.
- [X] T024 [P] [US1] Create AutoMapper profiles for mapping between entities and DTOs in `Maliev.PurchaseOrderService.Api/MappingProfiles/`.
- [X] T025 [P] [US1] Implement `IPurchaseOrderService` interface in `Api/Services/` with a `CreatePurchaseOrderAsync` method signature.
- [X] T026 [US1] Implement external service clients for `SupplierService`, `OrderService`, and `CurrencyService` in `Api/Services/Clients/` to validate IDs.
- [X] T027 [US1] Implement the core logic for `CreatePurchaseOrderAsync` in a new `PurchaseOrderService.cs`, including fetching data from external services and validating partial ordering logic against source quotation.
- [X] T028 [US1] Implement the Withholding Tax (WHT) calculation logic in a dedicated `WHTService.cs`.
- [X] T029 [US1] Implement document upload logic in a `DocumentManagementService.cs` that interacts with the `UploadServiceClient`.
- [X] T030 [US1] Implement event publishing for `PurchaseOrderCreated` to trigger PDF generation for internal POs.
- [X] T031 [P] [US1] Create `PurchaseOrdersController.cs` in `Api/Controllers/` with a `POST /purchase-orders/v1` endpoint.
- [X] T032 [US1] Write unit tests for `PurchaseOrderService` creation logic in `Tests/Unit/PurchaseOrderServiceTests.cs`.
- [X] T033 [P] [US1] Write integration tests for the `POST /purchase-orders/v1` endpoint in `Tests/Integration/PurchaseOrdersControllerTests.cs`, mocking external services.

---

## Phase 4: [US2] View and Search Purchase Orders

**User Story**: As a procurement team member, I need to view the details of a specific purchase order and search for orders based on various criteria.

- [X] T034 [P] [US2] Create DTOs for search filters and paged results in `Maliev.PurchaseOrderService.Api/DTOs/`.
- [X] T035 [P] [US2] Add `GetPurchaseOrderByIdAsync` and `SearchPurchaseOrdersAsync` methods to `IPurchaseOrderService` and its implementation.
- [X] T036 [US2] Implement the logic for retrieving a single purchase order, including its related items and documents, in `PurchaseOrderService.cs`.
- [X] T037 [US2] Implement the filtering and pagination logic for the search functionality in `PurchaseOrderService.cs`.
- [X] T038 [P] [US2] Add `GET /purchase-orders/v1/{id}` and `GET /purchase-orders/v1` endpoints to `PurchaseOrdersController.cs`.
- [X] T039 [US2] Write unit tests for the search and get-by-id logic in `Tests/Unit/PurchaseOrderServiceTests.cs`.
- [X] T040 [P] [US2] Write integration tests for the `GET` endpoints in `Tests/Integration/PurchaseOrdersControllerTests.cs`.

---

## Phase 5: [US3] Update and Cancel Purchase Orders

**User Story**: As a procurement team member, I need to update the details of an existing purchase order and cancel it if it's no longer needed.

- [X] T041 [P] [US3] Create DTO for purchase order updates (`UpdatePurchaseOrderRequest.cs`) in `Maliev.PurchaseOrderService.Api/DTOs/`.
- [X] T042 [P] [US3] Add `UpdatePurchaseOrderAsync` and `CancelPurchaseOrderAsync` methods to `IPurchaseOrderService` and its implementation.
- [X] T043 [US3] Implement logic for updating a PO in `PurchaseOrderService.cs`, including optimistic concurrency checks and re-triggering PDF generation for internal POs.
- [X] T044 [US3] Implement logic for canceling a PO in `PurchaseOrderService.cs`.
- [X] T045 [P] [US3] Add `PUT /purchase-orders/v1/{id}` and `POST /purchase-orders/v1/{id}/cancel` endpoints to `PurchaseOrdersController.cs`.
- [X] T046 [US3] Write unit tests for the update and cancel logic in `Tests/Unit/PurchaseOrderServiceTests.cs`.
- [X] T047 [P] [US3] Write integration tests for the `PUT` and `cancel` endpoints in `Tests/Integration/PurchaseOrdersControllerTests.cs`.

---

## Phase 6: Polish and Finalize

This phase addresses cross-cutting concerns and prepares the service for deployment.

- [X] T048 Configure health check endpoints `/purchase-orders/liveness` and `/purchase-orders/readiness` in `Program.cs`.
- [X] T049 Configure automatic database migration on startup in `Program.cs`.
- [X] T050 [P] Implement comprehensive audit trail logging for all CUD operations.
- [X] T050a [P] Implement a background job (or document the infrastructure requirement) to move Audit Logs older than 5 years to cold storage (e.g., GCS bucket) to satisfy FR-012.
- [X] T051 [P] Review and apply rate-limiting policies to critical endpoints.
- [X] T052 Verify all user roles and authorization policies are correctly implemented and tested.
- [X] T053 Finalize the `Dockerfile` healthcheck to point to the correct liveness endpoint (`/purchase-orders/liveness`).
- [X] T054 [P] Ensure 80% minimum test coverage is met and that `dotnet build` and `dotnet test` run with zero warnings.
- [ ] T055 **Manual Step**: Update the central .NET Aspire AppHost project to add the `Maliev.PurchaseOrderService.Api` for orchestration.

## Dependencies

- **Phase 1** must be complete before any other phase.
- **Phase 2** must be complete before **Phases 3, 4, 5**.
- **User Story Phases (3, 4, 5)** can be developed in parallel after Phase 2 is complete, although they may share DTOs and service methods.
- **Phase 6** should be completed last.

## Parallel Execution Examples

- **Setup (T002, T003, T004, T007, T008, T009, T010)** can largely be run in parallel.
- **Foundational (T013, T014, T015, T021)** can be run in parallel.
- **Within User Stories**, DTOs, controller endpoint definitions, and tests can often be created in parallel with service method implementation. For example, in **Phase 3**, tasks **T023, T024, T031, T032, T033** can be started concurrently.

## Implementation Strategy

The suggested approach is to implement phase-by-phase. However, after Phase 2 is complete, the user story phases (3, 4, 5) are designed to be largely independent and can be tackled in any order or in parallel to deliver value incrementally. The MVP would typically be the completion of Phase 3 ([US1]).