# Maliev.PurchaseOrderService Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-09-19

## Project Overview
PurchaseOrderService is a production-ready .NET 9 microservice for managing purchase orders (internal and external) with environment-specific integration to SupplierService, OrderService, CurrencyService, UploadService, and PdfService. Supports document management, automatic PDF generation for internal POs only, WHT calculations, customer PO tracking, Thailand tax compliance, CORS configuration for multiple environments, and comprehensive performance optimization.

## Active Technologies
- .NET 9.0 + ASP.NET Core 9.0, Entity Framework Core 9.0, PostgreSQL, Serilog.AspNetCore
- HttpClientFactory for external service integration
- JWT Bearer authentication with role-based authorization
- Docker containerization for Kubernetes deployment

## Project Structure
```
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
```

## External Service Integrations
- **SupplierService**: Supplier validation and address management
- **OrderService/QuotationService**: Order items derivation (read-only)
- **CurrencyService**: Currency validation and caching
- **UploadService**: Document management in GCS bucket
- **PdfService**: Automatic PDF generation for purchase orders

## Environment Configuration

### Service Endpoints (Environment Variables)
```bash
# External service URLs (populated via Google Secret Manager)
# Development Environment (Example)
SUPPLIER_SERVICE_URL=http://localhost:5001/suppliers
ORDER_SERVICE_URL=http://localhost:5002/orders
CURRENCY_SERVICE_URL=http://localhost:5003/currencies
UPLOAD_SERVICE_URL=http://localhost:5004/uploads
PDF_SERVICE_URL=http://localhost:5005/pdfs

# Production Environment (Configure via Google Secret Manager)
SUPPLIER_SERVICE_URL=${SECRET_SUPPLIER_SERVICE_URL}
ORDER_SERVICE_URL=${SECRET_ORDER_SERVICE_URL}
CURRENCY_SERVICE_URL=${SECRET_CURRENCY_SERVICE_URL}
UPLOAD_SERVICE_URL=${SECRET_UPLOAD_SERVICE_URL}
PDF_SERVICE_URL=${SECRET_PDF_SERVICE_URL}

# Database (Configure via Google Secret Manager)
DB_SERVER=${SECRET_DB_SERVER}
DB_PORT=${SECRET_DB_PORT}
DB_NAME=${SECRET_DB_NAME}
DB_USER=${SECRET_DB_USER}
DB_PASSWORD=${SECRET_DB_PASSWORD}

# JWT Configuration (Configure via Google Secret Manager)
JWT_SIGNING_KEY=${SECRET_JWT_SIGNING_KEY}
JWT_ISSUER=${SECRET_JWT_ISSUER}
JWT_AUDIENCE=${SECRET_JWT_AUDIENCE}
```

### CORS Configuration
```bash
# Development (Example)
CORS_ALLOWED_ORIGINS=http://localhost:3000,http://localhost:4200

# Staging/Production (Configure via Google Secret Manager)
CORS_ALLOWED_ORIGINS=${SECRET_CORS_ALLOWED_ORIGINS}
```

## Key Features
- **Purchase Order Management**: Full CRUD with external service integration
- **Document Management**: File upload/download/delete via UploadService
- **Automatic PDF Generation**: Event-driven PDF creation via PdfService for **internal POs only**
- **Environment Configuration**: Dynamic service endpoints via environment variables
- **CORS Support**: Environment-specific frontend origins (dev/staging/production)
- **Customer PO Tracking**: Optional customer purchase order numbers
- **Withholding Tax (WHT)**: Thailand tax regulations compliance
- **Event-driven Architecture**: Domain events for async processing (PDF generation)
- **Performance Optimization**: Caching, rate limiting, async I/O
- **Health Checks**: Kubernetes-ready liveness and readiness probes
- **Audit Trail**: Comprehensive logging for all operations including PDF generation
- **Role-based Access**: Employee/Manager/Procurement/Admin roles

## Commands
```bash
# Build and test (zero warnings enforced)
dotnet build Maliev.PurchaseOrderService.sln
dotnet test Maliev.PurchaseOrderService.sln --verbosity normal

# Database migrations (automatic on startup)
dotnet ef migrations add InitialCreate --project Maliev.PurchaseOrderService.Data
dotnet ef database update --project Maliev.PurchaseOrderService.Data

# Health checks
curl http://localhost:8080/purchaseorders/liveness
curl http://localhost:8080/purchaseorders/readiness

# API documentation (development only)
open http://localhost:8080/purchaseorders/swagger
```

## Code Style
- Follow .NET 9.0 conventions and MALIEV Microservices Constitution
- Test-First Development (TDD) approach mandatory
- 80% minimum test coverage for business-critical logic
- Zero warnings policy: `dotnet build` and `dotnet test` must produce no warnings
- Structured JSON logging to stdout via Serilog (no file logging)
- External service mocking for all integration tests
- Async/await pattern throughout for non-blocking I/O
- Environment-specific configuration via environment variables

## Constitutional Compliance
- **Service Autonomy**: Independent PostgreSQL database, API-only communication
- **Explicit Contracts**: OpenAPI/Swagger documentation for all endpoints
- **Test-First Development**: Red-Green-Refactor cycle, comprehensive mocking
- **Auditability**: Tamper-proof logs, health checks, traceable operations
- **Security**: JWT authentication, role-based authorization, data encryption
- **Simplicity**: YAGNI principle, stateless design, clear readable code

## Recent Changes
- 001-create-a-microservice: Created production-ready PurchaseOrderService with environment-specific configuration, document management, WHT calculations, automatic PDF generation for internal POs only, event-driven architecture, CORS configuration, performance optimization, health checks, and full constitutional compliance

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
