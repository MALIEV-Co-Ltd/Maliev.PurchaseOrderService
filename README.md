# Maliev.PurchaseOrderService

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.PurchaseOrderService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![Database](https://img.shields.io/badge/PostgreSQL-18-336791.svg)](https://www.postgresql.org/)
[![Tests](https://img.shields.io/badge/tests-26%2F26%20passing-brightgreen.svg)](https://github.com/MALIEV-Co-Ltd/Maliev.PurchaseOrderService)
[![License](https://img.shields.io/badge/license-Proprietary-red.svg)](LICENSE)

Procurement and purchase order management system for the MALIEV platform. Handles purchase requisitions, PO creation, approval workflows, supplier order tracking with full IAM integration, and event-driven communication via MessagingContracts.

---

## Architecture & Tech Stack

### Technology Stack
- **.NET 10.0**: ASP.NET Core Web API with C# 13
- **PostgreSQL 18**: Primary database with Entity Framework Core 10.x
- **Redis**: Distributed caching for supplier data and PO status
- **RabbitMQ**: Event-driven messaging via MassTransit 8.5.7
- **OpenTelemetry**: Structured logging, metrics, and distributed tracing
- **Testcontainers**: Integration testing with real PostgreSQL, Redis, RabbitMQ

### Project Structure
```
Maliev.PurchaseOrderService/
├── Maliev.PurchaseOrderService.Api/          # Presentation layer
│   ├── Controllers/                          # REST API endpoints
│   ├── Services/                             # Business logic
│   ├── Models/                               # DTOs (Request/Response)
│   └── Consumers/                            # MassTransit event consumers
├── Maliev.PurchaseOrderService.Data/         # Data access layer
│   ├── Entities/                             # EF Core entities
│   ├── Configurations/                       # Entity configurations
│   └── Migrations/                           # Database migrations
└── Maliev.PurchaseOrderService.Tests/        # Integration tests
    ├── Integration/                          # API integration tests
    └── Testing/                              # Test infrastructure
```

### Dependencies

**Databases:**
- **PostgreSQL 18**: PO headers, line items, goods receipts, approval workflow
- **Redis**: Caching for supplier data and PO status lookups

**Messaging:**
- **RabbitMQ**: Event publishing (PO lifecycle events) and consumption (supplier updates)

**External Services:**
- **IAM Service**: Authentication, authorization, and permission management
- **Supplier Service**: Supplier information and performance evaluation
- **Material Service**: Material/product master data and pricing
- **Inventory Service**: Goods receipt processing and stock updates
- **Accounting Service**: Invoice matching (3-way matching) and payment processing

---

## ⚠️ Constitution Rules

These rules are **non-negotiable** and apply to ALL Maliev microservices:

### Banned Libraries
| ❌ BANNED | ✅ USE INSTEAD |
|-----------|----------------|
| AutoMapper | Explicit manual mapping |
| FluentValidation | Data Annotations (`[Required]`, `[StringLength]`, etc.) |
| FluentAssertions | xUnit `Assert.*` methods |
| In-memory test DB | Testcontainers (real PostgreSQL) |
| `/src` or `/tests` folders | Flat project structure at repo root |

### Mandatory Practices
- **No Secrets in Code**: All secrets injected via Google Secret Manager (environment variables)
- **TreatWarningsAsErrors**: Enabled in all `.csproj` files - zero warnings tolerated
- **XML Documentation**: Required on ALL public methods, properties, and classes
- **MessagingContracts Only**: ALL events use `Maliev.MessagingContracts` package (no local events)
- **ServiceDefaults Integration**: Use `Maliev.Aspire.ServiceDefaults` for infrastructure patterns

---

## Key Features

### Procurement Workflow
- **Purchase Requisition**: Employee-initiated purchase requests
- **PO Creation**: Convert requisitions to formal purchase orders
- **Multi-Level Approval**: Configurable approval workflow based on amount thresholds
- **Supplier Integration**: Electronic PO transmission to suppliers
- **Goods Receipt**: Track partial and complete deliveries with quality inspection
- **3-Way Matching**: Automated PO, goods receipt, and invoice matching

### Purchase Order Lifecycle
```
Draft → Submitted → Approved/Rejected → Sent → Partially Received → Received → Closed
                                                                            ↓
                                                                        Cancelled
```

**Status Transitions:**
- **Draft**: PO created, editable
- **Submitted**: Awaiting approval
- **Approved**: Ready to send to supplier
- **Rejected**: Returned to requester for revision
- **Sent**: Transmitted to supplier
- **Partially Received**: Some items received
- **Received**: All items received
- **Closed**: PO completed (invoiced and paid)
- **Cancelled**: PO cancelled before completion

### Advanced Features
- **Blanket Orders**: Support for recurring purchases with release schedule
- **Price History**: Track price changes over time for cost analysis
- **Supplier Performance**: Delivery time, quality metrics, and compliance tracking
- **Contract Pricing**: Enforce contract terms and pricing agreements
- **Budget Integration**: PO commitment against department budgets
- **Document Attachment**: Link technical drawings, specifications, and certifications

### Event-Driven Integration
- **Events Published** (via MessagingContracts):
  - `PurchaseOrderCreatedEvent` - New PO created
  - `PurchaseOrderApprovedEvent` - PO approved for ordering
  - `PurchaseOrderSentEvent` - PO sent to supplier
  - `GoodsReceivedEvent` - Goods receipt recorded
  - `PurchaseOrderClosedEvent` - PO completed and closed

- **Events Consumed**:
  - `SupplierUpdatedEvent` - Update supplier information on POs
  - `MaterialPriceChangedEvent` - Alert when PO prices differ from current pricing

---

## Quick Start

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL 18 (local or via Kubernetes port-forward)
- Redis (optional, for caching)
- RabbitMQ (optional, for event messaging)

### Local Development

1. **Clone Repository**
   ```bash
   git clone https://github.com/MALIEV-Co-Ltd/Maliev.PurchaseOrderService.git
   cd Maliev.PurchaseOrderService
   ```

2. **Configure Database Connection**
   ```bash
   # Set connection string environment variable
   export ConnectionStrings__PurchaseOrderDbContext="Host=localhost;Port=5432;Database=purchase_order_app_db;Username=postgres;Password=<password>;"
   ```

3. **Apply Database Migrations**
   ```bash
   dotnet ef database update --project Maliev.PurchaseOrderService.Data
   ```

4. **Run the Service**
   ```bash
   cd Maliev.PurchaseOrderService.Api
   dotnet run
   ```

5. **Access API Documentation**
   - Scalar UI: http://localhost:5000/purchase-order/scalar
   - OpenAPI Spec: http://localhost:5000/purchase-order/openapi/v1.json
   - Health Check: http://localhost:5000/purchase-order/readiness

### Docker Deployment

```bash
# Build image
docker build -t maliev/purchase-order-service:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__PurchaseOrderDbContext="Host=postgres;Port=5432;Database=purchase_order_app_db;..." \
  -e Jwt__PublicKey="<base64-encoded-public-key>" \
  maliev/purchase-order-service:latest
```

---

## API Endpoints

All endpoints are prefixed with `/purchase-order` (configured via `UsePathBase("/purchase-order")`):

### Purchase Orders
- `GET /purchase-order/v1/purchase-orders` - List purchase orders (paginated)
- `POST /purchase-order/v1/purchase-orders` - Create new PO
- `GET /purchase-order/v1/purchase-orders/{id}` - Get PO details
- `PUT /purchase-order/v1/purchase-orders/{id}` - Update draft PO
- `DELETE /purchase-order/v1/purchase-orders/{id}` - Delete draft PO
- `POST /purchase-order/v1/purchase-orders/{id}/submit` - Submit for approval
- `POST /purchase-order/v1/purchase-orders/{id}/approve` - Approve PO
- `POST /purchase-order/v1/purchase-orders/{id}/reject` - Reject PO
- `POST /purchase-order/v1/purchase-orders/{id}/send` - Send to supplier
- `POST /purchase-order/v1/purchase-orders/{id}/cancel` - Cancel PO
- `POST /purchase-order/v1/purchase-orders/{id}/receive` - Record goods receipt
- `GET /purchase-order/v1/purchase-orders/supplier/{supplierId}` - Get supplier POs
- `GET /purchase-order/v1/purchase-orders/{id}/pdf` - Generate PO PDF

### Purchase Requisitions
- `GET /purchase-order/v1/requisitions` - List requisitions
- `POST /purchase-order/v1/requisitions` - Create requisition
- `GET /purchase-order/v1/requisitions/{id}` - Get requisition details
- `POST /purchase-order/v1/requisitions/{id}/approve` - Approve requisition
- `POST /purchase-order/v1/requisitions/{id}/convert` - Convert to PO

---

## Health & Monitoring

### Health Endpoints
- **Liveness**: `GET /purchase-order/liveness` - Service is running
- **Readiness**: `GET /purchase-order/readiness` - Service is ready (DB + dependencies healthy)

### Observability
- **Metrics**: Prometheus metrics at `/purchase-order/metrics`
- **Tracing**: OpenTelemetry distributed tracing to configured OTLP endpoint
- **Logging**: Structured logging with correlation IDs via ServiceDefaults

### Health Check Components
- PostgreSQL connection
- Redis cache availability
- RabbitMQ connection
- External service connectivity (IAM, Supplier, Material)

---

## Configuration

### Required Secrets (Google Secret Manager)
```
ConnectionStrings__PurchaseOrderDbContext - PostgreSQL connection string
Jwt__PublicKey  - Base64-encoded RSA-2048 public key (PEM format)
```

### Environment Variables
```bash
ConnectionStrings__PurchaseOrderDbContext="Host=postgres;Port=5432;Database=purchase_order_app_db;Username=app;Password=..."
ConnectionStrings__redis="redis:6379"
ConnectionStrings__rabbitmq="amqp://guest:guest@rabbitmq:5672"
Jwt__Issuer="https://dev.api.maliev.com/auth"
Jwt__Audience="https://dev.api.maliev.com"
ExternalServices__IAMService__BaseUrl="http://iam-service:8080"
ExternalServices__SupplierService__BaseUrl="http://supplier-service:8080"
ExternalServices__MaterialService__BaseUrl="http://material-service:8080"
```

### Configuration Files
- `appsettings.json` - Production settings (no secrets)
- `appsettings.Development.json` - Local development overrides
- `appsettings.Testing.json` - Test configuration with test keys

---

## IAM Integration

### Required Permissions
- `purchase_orders.read` - View purchase orders
- `purchase_orders.write` - Create and update purchase orders
- `purchase_orders.delete` - Delete draft purchase orders
- `purchase_orders.approve` - Approve purchase orders
- `purchase_orders.send` - Send POs to suppliers
- `purchase_orders.receive` - Record goods receipt
- `purchase_orders.cancel` - Cancel purchase orders
- `requisitions.read` - View purchase requisitions
- `requisitions.write` - Create purchase requisitions
- `requisitions.approve` - Approve purchase requisitions

### Predefined Roles
- **Requisitioner**: Create purchase requisitions (`requisitions.write`, `requisitions.read`)
- **Buyer**: Create and manage purchase orders (`purchase_orders.write`, `purchase_orders.read`, `purchase_orders.send`)
- **Procurement Manager**: Approve purchase orders (`purchase_orders.approve`, `purchase_orders.*`)
- **Warehouse Manager**: Record goods receipts (`purchase_orders.receive`, `purchase_orders.read`)
- **Finance**: View all POs for invoice matching (`purchase_orders.read`)

---

## Testing

### Test Coverage
**26/26 integration tests passing (100%)**

Test suites:
- **PurchaseOrderController Tests** (15 tests)
  - CRUD operations (create, read, update, delete)
  - Workflow transitions (submit, approve, reject, send, receive, cancel)
  - Authorization checks (permission-based)
- **RequisitionController Tests** (8 tests)
  - Requisition creation and approval
  - Conversion to purchase orders
- **IAM Registration Tests** (1 test)
  - Permission registration with IAM service on startup
- **Event Publishing Tests** (2 tests)
  - PurchaseOrderCreatedEvent
  - GoodsReceivedEvent

### Running Tests

```bash
# Run all tests
dotnet test Maliev.PurchaseOrderService.sln --verbosity normal

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~PurchaseOrderControllerTests"
```

### Test Infrastructure
- **Testcontainers**: Real PostgreSQL 18, Redis, RabbitMQ containers
- **WireMock.Net**: Mock external services (IAM, Supplier, Material)
- **MassTransit Test Harness**: Verify event publishing and consumption
- **xUnit**: Test framework with `Assert.*` assertions

---

## Database

### Database Schema

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `PurchaseOrders` - PO headers (supplier, dates, total, status, approval info)
- `PurchaseOrderItems` - Line items (material, quantity, unit price, delivery date)
- `GoodsReceipts` - Receipt records (PO reference, date, quantity, quality inspection)
- `PurchaseRequisitions` - Requisition requests (requester, department, justification)
- `POApprovals` - Approval workflow tracking (approver, status, timestamp, comments)

**Status Values:**
- `Draft`, `Submitted`, `Approved`, `Rejected`, `Sent`, `PartiallyReceived`, `Received`, `Closed`, `Cancelled`

### Database Migrations

```bash
# Port forward to PostgreSQL pod (MUST use pod, not service)
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432

# Set connection string environment variable
export ConnectionStrings__PurchaseOrderDbContext="Host=localhost;Port=5432;Database=purchase_order_app_db;Username=postgres;Password=<password>;"

# Create migration
dotnet ef migrations add MigrationName --project Maliev.PurchaseOrderService.Data

# Apply migration
dotnet ef database update --project Maliev.PurchaseOrderService.Data

# Rollback migration
dotnet ef database update PreviousMigrationName --project Maliev.PurchaseOrderService.Data
```

---

## Deployment

### Kubernetes Deployment

Service uses GitHub Actions workflows:
- `ci-develop.yml` - Deploy to `maliev-dev` namespace
- `ci-staging.yml` - Deploy to `maliev-staging` namespace
- `ci-main.yml` - Deploy to `maliev-prod` namespace

Deployments are managed via GitOps (ArgoCD) in the `maliev-gitops` repository.

### Port Forwarding

```bash
# Forward to service
kubectl port-forward -n maliev-dev svc/maliev-purchase-order-service 8080:8080

# Forward to PostgreSQL (for migrations)
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432

# Forward to Redis
kubectl port-forward -n maliev-dev svc/redis 6379:6379
```

### Logs

```bash
# Tail logs
kubectl logs -f deployment/maliev-purchase-order-service -n maliev-dev

# Get pod status
kubectl get pods -n maliev-dev | grep purchase-order-service

# Describe pod
kubectl describe pod <pod-name> -n maliev-dev
```

---

## Common Issues

### Issue: Tests fail with "Database connection string not configured"
**Solution**: Set `ConnectionStrings__PurchaseOrderDbContext` environment variable before running tests, or configure via User Secrets:
```bash
cd Maliev.PurchaseOrderService.Tests
dotnet user-secrets set "ConnectionStrings:PurchaseOrderDbContext" "Host=localhost;Port=5432;..."
```

### Issue: Migration fails with "Cannot connect to database"
**Solution**: Ensure PostgreSQL is accessible. If using Kubernetes, port-forward to the pod (NOT service):
```bash
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432
```

### Issue: Scalar UI returns 404
**Solution**: Scalar is disabled in production. Check environment is Development or Staging.

### Issue: All JWT validations fail
**Solution**: Ensure `Jwt:PublicKey` is correctly configured in Google Secret Manager and matches the AuthService private key.

### Issue: Events not publishing
**Solution**: Verify RabbitMQ connection string is configured and MessagingContracts package is up-to-date.

---

## Development Guidelines

### Adding New Endpoints
1. Create request/response models in `Models/`
2. Add validators using Data Annotations
3. Implement service logic in `Services/`
4. Add controller action in `Controllers/`
5. Write integration tests in `Tests/Integration/`
6. Update API documentation in README.md

### Adding New Database Entities
1. Create entity class in `Data/Entities/`
2. Add EF Core configuration in `Data/Configurations/`
3. Update `PurchaseOrderDbContext.cs` with DbSet
4. Create migration: `dotnet ef migrations add EntityName --project Maliev.PurchaseOrderService.Data`
5. Apply migration: `dotnet ef database update --project Maliev.PurchaseOrderService.Data`

### Code Style
- Follow .NET naming conventions
- Use async/await for all I/O operations
- Implement repository pattern for data access
- Use dependency injection for all services
- Add XML documentation comments for public APIs
- Include structured logging with correlation IDs

---

## Support

- **CLAUDE.md**: Service-specific development guidelines
- **ServiceDefaults Documentation**: `B:\maliev\Maliev.Aspire\Maliev.Aspire.ServiceDefaults\README.md`
- **MessagingContracts**: `B:\maliev\Maliev.MessagingContracts\README.md`
- **Test Summary**: `B:\maliev\all-services-test-summary.txt`

---

## License

**Proprietary** - Copyright © 2025 MALIEV Co., Ltd. All rights reserved.
