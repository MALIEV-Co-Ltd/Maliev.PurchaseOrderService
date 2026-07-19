# Maliev.PurchaseOrderService Agent Guidelines

This document provides instructions and context for AI agents working on the `Maliev.PurchaseOrderService` repository.

## 1. Project Overview
*   **Framework:** .NET 10.0 (ASP.NET Core Web API)
*   **Architecture:** Clean Architecture (Api → Application → Domain → Infrastructure)
*   **Database:** PostgreSQL (EF Core)
*   **Messaging:** MassTransit (RabbitMQ)
*   **Caching:** Redis
*   **Observability:** OpenTelemetry (Aspire Service Defaults)

### Workspace Structure
```
Maliev.PurchaseOrderService/
├── Maliev.PurchaseOrderService.Api/           # Controllers, Consumers, Middleware
├── Maliev.PurchaseOrderService.Application/   # Use cases, DTOs, Interfaces, Handlers
├── Maliev.PurchaseOrderService.Domain/        # Entities, value objects, domain interfaces
├── Maliev.PurchaseOrderService.Infrastructure/ # EF Core DbContext, repositories, HTTP clients
├── Maliev.PurchaseOrderService.Tests/         # Unit + Integration tests (xUnit)
├── Directory.Build.props                      # Central package versioning
└── Maliev.PurchaseOrderService.slnx           # Solution file (.slnx preferred over .sln)
```

## 2. Build, Lint & Test
Always ensure the codebase is buildable and tests pass before finishing a task.

### Commands
All commands run from within this service directory (`B:\maliev\Maliev.PurchaseOrderService`).

```powershell
# Build (treats warnings as errors — all must be fixed)
dotnet build Maliev.PurchaseOrderService.slnx

# Run all tests
dotnet test Maliev.PurchaseOrderService.slnx --verbosity normal

# Run a single test method
dotnet test --filter "FullyQualifiedName~PurchaseOrdersControllerTests.CreatePurchaseOrder_WithValidRequest_ReturnsCreatedOrder"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~PurchaseOrdersControllerTests"

# Run with code coverage
dotnet test Maliev.PurchaseOrderService.slnx --collect:"XPlat Code Coverage"

# Format check
dotnet format Maliev.PurchaseOrderService.slnx

# EF Core migrations (Infrastructure project only)
dotnet ef migrations add <Name> --project Maliev.PurchaseOrderService.Infrastructure --startup-project Maliev.PurchaseOrderService.Infrastructure
```

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

### Testing Rules
- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## 3. Code Style & Conventions

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.PurchaseOrderService.Api.Services;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `CreatePurchaseOrderAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IPurchaseOrderService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `purchaseorder.purchase-orders.create`, `purchaseorder.purchase-orders.update`
  - Invalid: `purchaseorder.purchase-order.create` (singular), `purchaseorder.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("purchase-order/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {PurchaseOrderId}", purchaseOrderId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **JSON**: Check existing conventions in this service for naming policy
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned
- **Ownership checks**: Treat `roles.purchase-order.employee` and legacy/simple `employee` as the same employee role; employee-role access must stay scoped to purchase orders whose `CreatedBy` equals the authenticated user id.
- **Cross-boundary DTO rule**: Before changing controllers, service clients, DTOs, events, or BFF payloads, verify request/response DTOs, JSON property names, messaging schemas, and tests that assert the actual wire shape.

### Data Access (EF Core)
*   **Entities:** Located in `Maliev.PurchaseOrderService.Domain.Entities`.
*   **Configuration:** Use Fluent API in `IEntityTypeConfiguration<T>`.
*   **Migrations:** Managed via `dotnet ef migrations` (target Infrastructure project only).
*   **Querying:** Use `AsNoTracking()` for read-only queries to improve performance.

## 4. Dependencies & External Services
*   **External Calls:** Use typed HTTP Clients (e.g., `SupplierServiceClient`) located in `Api/ExternalServices`.
*   **Messaging:** Publish events using `IPublishEndpoint` from MassTransit. Event definitions are in `Maliev.MessagingContracts`.

## 5. Pre-commit Hooks
This repo uses `pre-commit` to enforce:
*   `check-yaml`
*   `yamllint`
*   `dotnet-format`
*   `verify-build`

If you encounter issues, ensure your code is formatted and builds in Release mode.

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/purchase-order/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("purchaseorder.purchase-orders.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (`/purchase-order`)
- **Scalar docs**: Configured at `/purchase-order/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```
  dotnet ef migrations add <Name> --project Maliev.PurchaseOrderService.Infrastructure --startup-project Maliev.PurchaseOrderService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead

## Git Rules

- This is an independent git repo. All git commands must run from within `B:\maliev\Maliev.PurchaseOrderService`
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked
