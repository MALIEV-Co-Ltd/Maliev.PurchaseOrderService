# Maliev.PurchaseOrderService Migration Status

This document outlines the plan and progress for migrating the `PurchaseOrderService` to a modern, multi-project, production-ready .NET 9 solution.

## Part 1: Triage and Mode Selection
- **Status**: Initial Migration

## Part 2: Mandatory Execution Plan

### Step 1: Plan and Dynamic Discovery
- **Scan Source Projects**: Completed. Identified the following projects:
    - `Maliev.PurchaseOrderService.Api` (ASP.NET Core Web API)
    - `Maliev.PurchaseOrderService.Common` (Class Library)
    - `Maliev.PurchaseOrderService.Data` (Class Library, Data Access Layer)
    - `Maliev.PurchaseOrderService.Tests` (Test Project)
- **Dependency Graph**:
    - `Maliev.PurchaseOrderService.Api` -> `Maliev.PurchaseOrderService.Common`
    - `Maliev.PurchaseOrderService.Api` -> `Maliev.PurchaseOrderService.Data`
    - `Maliev.PurchaseOrderService.Tests` -> `Maliev.PurchaseOrderService.Api`
    - External dependencies (will be re-evaluated/replaced): `Maliev.AuthService.JwtSecurity`, `Maliev.LoggerService.NLog`, `Maliev.Entities`, `Maliev.Middleware.SwaggerAuthorized`
- **Create To-Do List**: This `migration-status.md` file.
- **Update `.gitignore`**: Pending.

### Step 2: Create and Clean Project Skeletons
- For each identified project, create a new .NET 9 project, delete boilerplate files, and add to the solution.
    - [ ] Create `Maliev.PurchaseOrderService.Api.csproj` (net9.0)
    - [ ] Create `Maliev.PurchaseOrderService.Common.csproj` (net9.0)
    - [ ] Create `Maliev.PurchaseOrderService.Data.csproj` (net9.0)
    - [ ] Create `Maliev.PurchaseOrderService.Tests.csproj` (net9.0)

### Step 3: Establish Project References
- Based on the dependency graph, add correct `<ProjectReference>` tags.
    - [ ] Add reference from `Maliev.PurchaseOrderService.Api` to `Maliev.PurchaseOrderService.Common`
    - [ ] Add reference from `Maliev.PurchaseOrderService.Api` to `Maliev.PurchaseOrderService.Data`
    - [ ] Add reference from `Maliev.PurchaseOrderService.Tests` to `Maliev.PurchaseOrderService.Api`

### Step 4: Re-implement Supporting Libraries
- Analyze source of supporting libraries (e.g., `.Common`) and write new, modernized code.
    - [ ] Re-implement `Maliev.PurchaseOrderService.Common`
    - [ ] Re-implement `Maliev.PurchaseOrderService.Data` (Entities, DbContext, etc.)

### Step 5: Implement Core Functionality and Replicate `Program.cs`
#### 5.1 - Code Generation
- Analyze source `DbContext`'s `OnModelCreating` for schema.
- Generate new, modern code for Entities, DTOs, `IPurchaseOrderServiceService`, and "thin" Controllers.
    - [ ] Generate Entities based on `DbContext` schema
    - [ ] Generate DTOs
    - [ ] Implement `IPurchaseOrderServiceService` interface and its implementation
    - [ ] Implement "thin" Controllers

#### 5.2 - Replicate `Program.cs` from the Reference Project
- Analyze `reference_project`'s `Program.cs` and replicate patterns exactly.
    - [ ] Replicate Service Registration Order
    - [ ] Replicate Authentication (`AddAuthentication`, `AddJwtBearer`)
    - [ ] Replicate API Versioning (`AddApiExplorer`)
    - [ ] Replicate Swagger Configuration
    - [ ] Replicate CORS
    - [ ] Replicate Exception Handling
    - [ ] Replicate Middleware Pipeline Order
    - [ ] Replicate Health Checks (if present in reference)

### Step 6: Write Comprehensive Unit Tests
- Write new tests for implemented functionality.
#### 6.1 - Service Layer Tests
    - [ ] Create unit tests for `PurchaseOrderServiceService` using mocking framework.
#### 6.2 - Controller Tests
    - [ ] Create unit tests for Controllers using mocking framework.

### Step 7: Configure Local Secrets
- Automate `dotnet user-secrets set` commands.
    - [ ] Set `Jwt:Issuer`
    - [ ] Set `Jwt:Audience`
    - [ ] Set `JwtSecurityKey`
    - [ ] Set `ConnectionString`

### Step 8: Final Verification
- Build the Solution: Resolve all build errors and warnings.
    - [ ] Run `dotnet build`
- Run All Tests: All new tests must pass.
    - [ ] Run `dotnet test`

### Step 9: API Standardization and Documentation
- Standardize all API routes.
    - [ ] Ensure global base path and explicit controller routes.
- Generate `GEMINI.md` and update `README.md`.
    - [ ] Generate `GEMINI.md`
    - [ ] Update `README.md`
- Present `ACTION REQUIRED` block with `gcloud secrets` commands.
    - [ ] Present `gcloud secrets` commands
