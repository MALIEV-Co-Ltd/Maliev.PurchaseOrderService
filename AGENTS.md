# Maliev.PurchaseOrderService Agent Guidelines

This document provides instructions and context for AI agents working on the `Maliev.PurchaseOrderService` repository.

## 1. Project Overview
*   **Framework:** .NET 10.0 (ASP.NET Core Web API)
*   **Architecture:** Layered (Api, Data, Common, Tests) with some Clean Architecture principles.
*   **Database:** PostgreSQL (EF Core)
*   **Messaging:** MassTransit (RabbitMQ)
*   **Caching:** Redis
*   **Observability:** OpenTelemetry (Aspire Service Defaults)

## 2. Build, Lint & Test
Always ensure the codebase is buildable and tests pass before finishing a task.

### Commands
*   **Build:** `dotnet build`
*   **Format/Lint:** `dotnet format` (Runs automatically via pre-commit)
*   **Run Tests:** `dotnet test`
*   **Run Single Test:**
    ```bash
    dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"
    # Example:
    # dotnet test --filter "FullyQualifiedName~Maliev.PurchaseOrderService.Tests.Integration.PurchaseOrdersControllerTests.CreatePurchaseOrder_WithValidRequest_ReturnsCreatedOrder"
    ```

### Testing Strategy
*   **Unit Tests (`Tests/Unit`):** Use xUnit and Moq. Test services and logic in isolation.
*   **Integration Tests (`Tests/Integration`):** Use `WebApplicationFactory`, `Testcontainers` (Postgres, Redis, RabbitMQ), and `WireMock.Net` for external HTTP dependencies.
*   **Conventions:**
    *   Naming: `MethodName_StateUnderTest_ExpectedBehavior` (e.g., `CreatePurchaseOrder_WithValidRequest_ReturnsCreatedOrder`).
    *   Arrange/Act/Assert comments are encouraged in complex tests.

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Minimum 80% code coverage
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## 3. Code Style & Conventions

### General
*   **Namespaces:** Use file-scoped namespaces (e.g., `namespace Maliev.PurchaseOrderService.Api.Services;`).
*   **Nullable Types:** Enabled (`<Nullable>enable</Nullable>`). Use `?` for optional types and handle nulls defensively.
*   **Async/Await:** Use `async Task` for I/O bound operations. Avoid `Result` or `Wait()`.
*   **Dependency Injection:** Use constructor injection. Interface-based programming is preferred for services.

### API Controllers
*   **Routing:** `[Route("purchase-order/v{version:apiVersion}/[controller]")]`
*   **Versioning:** Use `[Asp.Versioning.ApiVersion("1.0")]`.
*   **Attributes:** Explicitly define `[ProducesResponseType]` for all return paths.
*   **Authorization:** Use `[Authorize]` and `[RequirePermission(...)]` for granular access control.
*   **Error Handling:** Catch exceptions and return appropriate HTTP status codes (e.g., `NotFound`, `BadRequest`, `Conflict`). Log exceptions using `ILogger`.

### Data Access (EF Core)
*   **Entities:** Located in `Maliev.PurchaseOrderService.Data.Entities`.
*   **Configuration:** Use Fluent API in `DbContext` or `IEntityTypeConfiguration`.
*   **Migrations:** Managed via `dotnet ef migrations`.
*   **Querying:** Use `AsNoTracking()` for read-only queries to improve performance.

### Naming Conventions
*   **Classes/Methods:** PascalCase (e.g., `PurchaseOrderService`, `CreatePurchaseOrder`).
*   **Variables/Parameters:** camelCase (e.g., `purchaseOrderId`, `cancellationToken`).
*   **Private Fields:** Underscore + camelCase (e.g., `_context`, `_logger`).
*   **Interfaces:** Prefix with 'I' (e.g., `IPurchaseOrderService`).

### Documentation
*   **XML Comments:** Required for Controllers (Endpoints) and Public API contracts (DTOs).
*   **Format:**
    ```csharp
    /// <summary>
    /// Brief description of the method.
    /// </summary>
    /// <param name="id">Description of parameter.</param>
    /// <returns>Description of return value.</returns>
    ```

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


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
