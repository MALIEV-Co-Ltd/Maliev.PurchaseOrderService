# Implementation Plan: PurchaseOrderService Microservice

**Branch**: `001-create-a-microservice` | **Date**: 2025-09-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-create-a-microservice/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code or `AGENTS.md` for opencode).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
Primary requirement: Create a microservice for managing purchase orders (internal and external) with integration to SupplierService, OrderService, CurrencyService, UploadService, and PdfService. Purchase orders are linked to existing suppliers, orders, and currencies, with items derived from external services. Includes document management with file uploads to UploadService, automatic PDF generation via PdfService with event-driven processing, customer PO number tracking, Withholding Tax (WHT) calculation and validation according to Thailand tax regulations. Full CRUD operations on purchase orders, independent database, JWT authentication, role-based access control, audit trail, and optimistic concurrency control.

Technical approach: .NET 9 WebAPI microservice with Entity Framework Core, PostgreSQL database, JWT Bearer authentication, external service integration patterns for five services, document management with UploadService integration, automatic PDF generation with PdfService integration using event-driven architecture, WHT calculation engine with Thailand tax compliance, Serilog logging, containerization with Docker, following layered architecture (API → Application → Domain → Data).

## Technical Context
**Language/Version**: .NET 9.0
**Primary Dependencies**: ASP.NET Core 9.0, Entity Framework Core 9.0, PostgreSQL, Serilog.AspNetCore, HttpClientFactory for external services
**Storage**: PostgreSQL database (purchaseorder_app_db)
**Testing**: xUnit, Moq, FluentAssertions, minimum 80% coverage
**Target Platform**: Linux containers, Kubernetes deployment
**Project Type**: single (microservice API only)

**Project Structure**:
- `Maliev.PurchaseOrderService.Api` → main WebAPI project
- `Maliev.PurchaseOrderService.Data` → EF Core DbContext, entities, migrations
- `Maliev.PurchaseOrderService.Tests` → unit and integration tests

**Architecture & Design**: Layered architecture (API → Application → Domain → Data), stateless microservice with all persistent state in PostgreSQL, optimistic concurrency control, comprehensive audit trail, DTOs for API input/output mapped to domain entities

**Environment Configuration**:
- Database: `ConnectionStrings__PurchaseOrderDbContext` (purchaseorder_app_db)
- Service Endpoints: Environment variables via Google Secret Manager with structured configuration
  - `ExternalServices__SupplierService__BaseUrl` → Service base URL including service path (e.g., `https://{BaseUrl}/suppliers`)
  - `ExternalServices__OrderService__BaseUrl` → Service base URL including service path (e.g., `https://{BaseUrl}/orders`)
  - `ExternalServices__CurrencyService__BaseUrl` → Service base URL including service path (e.g., `https://{BaseUrl}/currencies`)
  - `ExternalServices__UploadService__BaseUrl` → Service base URL including service path (e.g., `https://{BaseUrl}/uploads`)
  - `ExternalServices__PdfService__BaseUrl` → Service base URL including service path (e.g., `https://{BaseUrl}/pdfs`)
  - Program.cs adds `/v1` suffix to create final URLs like `https://{BaseUrl}/orders/v1`
- Timeouts: `ExternalServices__{ServiceName}__TimeoutInSeconds` (default 180 seconds)
- JWT: Signing keys via environment variables
- CORS: Environment-specific allowed origins

**CORS Configuration**:
- Development: `https://dev.intranet.maliev.com`, `https://dev.www.maliev.com`
- Staging: `https://staging.intranet.maliev.com`, `https://staging.www.maliev.com`
- Production: `https://intranet.maliev.com`, `https://www.maliev.com`
- Methods: `GET, POST, PUT, DELETE, PATCH, OPTIONS`
- Headers: `Authorization, Content-Type`

**API Design**:
- **API Version**: All endpoints must use `/purchaseorders/v1` where v1 is the API version
- Swagger UI: `/purchaseorders/swagger` (development only)
- Health Endpoints: `/purchaseorders/liveness`, `/purchaseorders/readiness`
- Automatic database migrations on startup
- Rate limiting for critical endpoints
- Zero warnings policy for build and test

**Performance Goals**: Support concurrent access, optimized EF queries, efficient caching (in-memory/Redis), resilient external service integration, fast WHT calculations, <200ms API response time, efficient memory usage, async endpoints with non-blocking I/O

**Constraints**: JSON stdout logging only via Serilog, external service timeout handling, accurate financial calculations, environment-specific service endpoints, zero warnings in build

**Scale/Scope**: Enterprise procurement team usage, role-based access (employee/manager/procurement/admin), external service dependencies with environment-specific endpoints, Thailand tax compliance requirements, event-driven PDF generation for internal POs only

**External Integrations**: All service endpoints loaded from environment variables (no hardcoded URLs)
- **SupplierService**: `/suppliers/v1` - supplier validation and address management
- **OrderService**: `/orders/v1` - order items derivation (read-only from OrderService/QuotationService)
- **CurrencyService**: `/currencies/v1` - currency validation and caching
- **UploadService**: `/uploads/v1` - document management in GCS bucket
- **PdfService**: `/pdfs/v1` - automatic PDF generation for internal POs only
- **AuthenticationService**: `/auth/v1` - JWT token validation and user authentication

**Financial Features**: Withholding Tax (WHT) calculation according to Thailand tax regulations, validation, and audit trail
**Document Management**: File uploads to UploadService, automatic PDF generation via PdfService with event-driven processing for **internal POs only**, customer PO number tracking, document categorization, access control
**Constitutional Compliance**: Full adherence to MALIEV Microservices Constitution v1.0 including service autonomy, explicit contracts, test-first development, auditability, security, and maintainability principles

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**MALIEV Microservices Constitution Compliance Verification:**

### I. Service Autonomy (NON-NEGOTIABLE)
- ✅ **Own database**: PostgreSQL database (purchaseorder_app_db) independent from other services
- ✅ **Own domain logic**: Purchase order management with external service integration via APIs only
- ✅ **API-only communication**: Integration with SupplierService, OrderService, CurrencyService, UploadService, PdfService via HTTP APIs
- ✅ **No direct database access**: No access to other services' databases

### II. Explicit Contracts
- ✅ **OpenAPI documentation**: Complete Swagger/OpenAPI 3.0 specification for all endpoints
- ✅ **Contract versioning**: API versioned as MAJOR.MINOR format
- ✅ **Backward compatibility**: Schema changes require migration strategy

### III. Test-First Development (NON-NEGOTIABLE)
- ✅ **Unit tests mandatory**: 80% minimum coverage for business-critical logic
- ✅ **Integration tests**: Required for all external service interactions
- ✅ **Red-Green-Refactor**: Tests written before implementation
- ✅ **External service mocking**: Comprehensive mocking for SupplierService, OrderService, CurrencyService, UploadService, PdfService

### IV. Auditability & Observability
- ✅ **Structured logging**: JSON format to stdout via Serilog
- ✅ **Audit trail**: Tamper-proof logs for all operations including external service calls, WHT calculations, document operations, PDF generation events
- ✅ **Health checks**: Liveness and readiness probes for Kubernetes
- ✅ **Traceable operations**: User/action tracking in all logs

### V. Security & Compliance
- ✅ **JWT authentication**: Bearer token authentication for all endpoints
- ✅ **Role-based authorization**: Employee/Manager/Procurement/Admin roles enforced
- ✅ **Data encryption**: Sensitive financial data encrypted at rest
- ✅ **Regulatory compliance**: Thailand tax regulations for withholding tax calculations

### VI. Simplicity & Maintainability
- ✅ **YAGNI principle**: Building only required purchase order functionality
- ✅ **Stateless design**: All state maintained in database
- ✅ **Clear code**: Readable implementation over optimizations
- ✅ **Documented dependencies**: All external service integrations well-documented

## Project Structure

### Documentation (this feature)
```
specs/001-create-a-microservice/
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Single microservice project structure
Maliev.PurchaseOrderService.Api/     # Main WebAPI project
├── Controllers/                     # REST API controllers
├── DTOs/                           # Data Transfer Objects
├── Services/                       # Business logic services
├── MappingProfiles/               # AutoMapper profiles
├── Program.cs                     # Application entry point
└── Dockerfile                     # Container definition

Maliev.PurchaseOrderService.Data/   # Data access layer
├── Entities/                      # Domain entities
├── PurchaseOrderContext.cs        # DbContext
└── Migrations/                    # EF Core migrations

Maliev.PurchaseOrderService.Common/ # Shared models/enums
└── Enumerations/                  # Shared enums

Maliev.PurchaseOrderService.Tests/  # Test project
├── Controllers/                   # Controller tests
├── Services/                     # Service tests
└── Integration/                  # Integration tests

Maliev.PurchaseOrderService.sln     # Solution file
```

**Structure Decision**: Single microservice project following .NET solution conventions

## Phase 0: Outline & Research
All technical context is well-defined from the provided implementation plan. No NEEDS CLARIFICATION items remain from the specification analysis.

**Research Tasks**:
1. .NET 9 WebAPI best practices for microservices
2. Entity Framework Core optimistic concurrency patterns
3. JWT authentication implementation with role-based authorization
4. Serilog structured logging configuration for containers
5. PostgreSQL connection and performance optimization
6. Docker containerization for .NET 9 applications

**Output**: research.md documenting technology decisions and best practices

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

**Data Model Entities**:
- PurchaseOrder (main aggregate root with external references to SupplierService, OrderService, CurrencyService, and CustomerPO number)
- OrderItem (read-only, derived from external services)
- Address (shipping/billing addresses)
- PurchaseOrderFile (document management with UploadService integration, including generated PDFs)
- AuditLog (audit trail tracking including external service calls, WHT calculations, document operations, and PDF generation events)
- DomainEvent (event sourcing for PurchaseOrderCreated/Updated events to trigger PDF generation)

**API Contracts**:
- Purchase Orders CRUD endpoints with five external service integration, WHT support, and CustomerPO tracking
- Order Items read-only endpoints with cache refresh capabilities
- Address management endpoints
- Document management endpoints (upload, download, delete files via UploadService including generated PDFs)
- Search and filtering endpoints (by SupplierID, OrderID, CurrencyID, CustomerPO)
- WHT calculation and validation endpoints with Thailand tax compliance
- External service integration patterns for SupplierService, OrderService, CurrencyService, UploadService, and PdfService
- Event publishing endpoints for domain events (PurchaseOrderCreated/Updated)
- Authentication/authorization endpoints

**Contract Tests**: REST API schema validation tests for all endpoints including external service error scenarios, WHT validation, document upload/download, and PDF generation workflows

**Integration Tests**: User story validation scenarios including external service integration patterns, WHT calculation workflows, document management workflows, and event-driven PDF generation

**Agent Context Update**: Update CLAUDE.md with PurchaseOrderService, external service integration, document management, automatic PDF generation, and WHT functionality context

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, CLAUDE.md

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
- Generate solution and project structure tasks following constitutional requirements and .NET 9 naming conventions (Maliev.PurchaseOrderService.Api/Data/Tests)
- Create Entity Framework entities and DbContext with five external service integration, WHT fields, document management, event sourcing, and internal/external PO classification (Constitution: Service Autonomy)
- Implement repository pattern for data access with caching (in-memory/Redis) (Constitution: Simplicity & Maintainability)
- Create DTOs and AutoMapper profiles for external service data, WHT calculations, document management, and PDF generation
- Build REST API controllers with CRUD operations, five external service integration, WHT support, document endpoints, event publishing, and environment-specific configuration (Constitution: Explicit Contracts)
- Implement comprehensive OpenAPI/Swagger documentation for all endpoints with development-only access at `/purchaseorders/swagger` (Constitution: Explicit Contracts)
- Implement environment-specific configuration management for all service endpoints via environment variables (Google Secret Manager integration)
- Implement CORS configuration for environment-specific frontend origins (dev/staging/production)
- Implement document management service with UploadService integration (upload, download, delete)
- Implement automatic PDF generation service with PdfService integration and event-driven processing for **internal POs only**
- Implement event-driven architecture for PurchaseOrderCreated/Updated events with async PDF generation for internal POs
- Implement external service clients with HttpClientFactory and versioned endpoint configuration (Constitution: Service Autonomy):
  - SupplierService client: `/suppliers/v1` endpoints
  - OrderService client: `/orders/v1` endpoints
  - CurrencyService client: `/currencies/v1` endpoints
  - UploadService client: `/uploads/v1` endpoints
  - PdfService client: `/pdfs/v1` endpoints
  - AuthenticationService client: `/auth/v1` endpoints
- Add resilience patterns (circuit breakers, retry policies, timeouts) for all external services
- Implement JWT authentication and role-based authorization with environment-specific configuration (Constitution: Security & Compliance)
- Implement API rate limiting for critical endpoints and performance optimization
- Implement caching strategies (in-memory/Redis) for frequently accessed data
- Implement WHT calculation engine with Thailand tax regulations validation and legal limit checks (Constitution: Security & Compliance)
- Add comprehensive audit trail functionality including external service call logging, WHT operations, document operations, and PDF generation events (Constitution: Auditability & Observability)
- Create optimistic concurrency control with rate limiting safeguards
- Implement health check endpoints at `/purchaseorders/liveness` and `/purchaseorders/readiness`
- Implement automatic database migration execution on startup
- Write comprehensive unit and integration tests with external service mocking, WHT scenarios, document management workflows, PDF generation workflows for internal POs, and environment configuration testing (Constitution: Test-First Development)
- Ensure 80% minimum test coverage for business-critical logic with zero warnings policy (Constitution: Test-First Development)
- Configure Docker containerization with environment-based configuration (Constitution: Deployment & Operations Standards)
- Set up health checks and monitoring including external service health (Constitution: Auditability & Observability)
- Implement structured JSON logging to stdout via Serilog with no file logging (Constitution: Auditability & Observability)
- Implement async endpoints with non-blocking I/O for performance optimization
- Enforce zero warnings policy for `dotnet build` and `dotnet test`

**Ordering Strategy**:
- TDD order: Tests before implementation (Constitution: Test-First Development NON-NEGOTIABLE)
- Constitutional compliance verification at each phase gate
- Environment configuration setup early (service endpoints, database, JWT, CORS) for all environments
- External service integration setup early (clients, contracts for all five services with environment endpoints) ensuring service autonomy
- OpenAPI/Swagger documentation creation alongside endpoint development
- Event-driven architecture implementation early for PDF generation workflows
- Document management implementation with UploadService integration and comprehensive testing
- Automatic PDF generation implementation with PdfService integration and event processing for internal POs only
- WHT calculation engine implementation with Thailand compliance and comprehensive testing
- CORS and rate limiting implementation for production readiness
- Performance optimization (caching, async operations, memory efficiency)
- Bottom-up: Data layer → External Services (Supplier/Order/Currency/Upload/Pdf) → Event Architecture → Document Management → PDF Generation (Internal POs) → WHT Engine → Services → API controllers
- Authentication and authorization setup early for security compliance
- Audit trail and structured logging implementation throughout including PDF generation events
- Health check endpoints implementation for Kubernetes deployment
- Automatic database migration setup for startup execution
- Zero warnings enforcement throughout development process
- Mark [P] for parallel tasks (independent components)
- Constitutional compliance review before task completion

**Estimated Output**: 75-80 numbered, ordered tasks in tasks.md

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)
**Phase 4**: Implementation (execute tasks.md following constitutional principles)
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*No constitutional violations identified. Enhanced complexity due to:*
- Environment-specific service endpoint configuration across dev/staging/production
- CORS configuration for multiple frontend environments
- Event-driven PDF generation for internal POs only (business logic complexity)
- Performance optimization requirements (caching, rate limiting, async I/O)
- Zero warnings policy enforcement
- Comprehensive health check implementation for Kubernetes deployment
- Automatic database migration execution on startup

## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command) - Updated with API versioning and external service endpoints
- [x] Phase 1: Design complete (/plan command) - All contracts updated with versioned endpoints (/purchaseorders/v1, external services /v1)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [x] Phase 3: Tasks generated (/tasks command) - ALL 90 TASKS COMPLETED (100%)
- [x] Phase 4: Implementation complete - Production-ready microservice with comprehensive features
- [x] Phase 5: Validation passed - Application starts, APIs respond, tests pass, quickstart verified

**Gate Status**:
- [x] Initial Constitution Check: PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved
- [x] Complexity deviations documented

---
*Based on Constitution template - See `.specify/memory/constitution.md`*