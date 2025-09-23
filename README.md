# Purchase Order Service

A production-ready .NET 9.0 microservice for managing purchase orders with comprehensive external service integration, document management, and automated workflows.

## 🚀 Quick Start

```bash
# Clone and setup
git clone <repository-url>
cd Maliev.PurchaseOrderService

# Set environment variables (see Development Setup)
export ConnectionStrings__PurchaseOrderDbContext="Server=localhost;Port=5432;Database=purchaseorder_app_db;Username=postgres;Password=your_password"
export Jwt__SecurityKey="your-jwt-signing-key-minimum-256-bits"

# Run database migrations
dotnet ef database update --project Maliev.PurchaseOrderService.Data

# Start the application
dotnet run --project Maliev.PurchaseOrderService.Api --environment Development
```

## 📋 Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Development Setup](#development-setup)
- [Deployment](#deployment)
- [API Documentation](#api-documentation)
- [Business Logic](#business-logic)
- [External Integrations](#external-integrations)
- [Security](#security)
- [Monitoring](#monitoring)

## 🌟 Overview

The Purchase Order Service manages the complete lifecycle of purchase orders for Maliev Co. Ltd., supporting both internal company operations and external client projects. It integrates with multiple external services and provides automated document generation, tax calculations, and comprehensive audit trails.

### Key Capabilities

- **Full CRUD Operations** for purchase orders with approval workflows
- **Document Management** with 50MB file uploads and virus scanning
- **Automatic PDF Generation** for internal purchase orders only
- **WHT (Withholding Tax) Calculations** for Thailand tax compliance
- **External Service Integration** with Supplier, Order, Currency, Upload, and PDF services
- **Domain Events** for asynchronous processing and audit trails
- **Role-Based Access Control** (Employee, Manager, Procurement, Admin)

## ✨ Features

### Core Features
- ✅ Purchase order CRUD with business validation
- ✅ Approval workflow (Pending → Approved → Ordered → Delivered)
- ✅ Document upload/download with Google Cloud Storage
- ✅ Automatic PDF generation for internal orders
- ✅ WHT calculation for Thai tax compliance
- ✅ Customer PO number tracking
- ✅ Comprehensive audit logging

### Technical Features
- ✅ .NET 9.0 with minimal APIs and ASP.NET Core
- ✅ Entity Framework Core with PostgreSQL
- ✅ JWT Bearer authentication with role-based authorization
- ✅ Circuit breaker pattern for external service resilience
- ✅ Structured JSON logging with Serilog
- ✅ Health checks for Kubernetes deployment
- ✅ OpenAPI/Swagger documentation
- ✅ Comprehensive integration tests (120+ test methods)

## 🏗️ Architecture

### Technology Stack
- **Runtime**: .NET 9.0
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Logging**: Serilog with structured JSON output
- **Documentation**: OpenAPI/Swagger
- **Testing**: xUnit with FluentAssertions
- **Deployment**: Docker containers on Kubernetes

### External Services
- **SupplierService**: Supplier validation and address management
- **OrderService**: Order items derivation and tracking
- **CurrencyService**: Currency validation with caching
- **UploadService**: Document management in Google Cloud Storage
- **PdfService**: Automated PDF generation for internal orders
- **AuthService**: User authentication and authorization

### Domain Model

```
PurchaseOrder (Aggregate Root)
├── OrderItems[] (Entity)
├── Addresses[] (Entity)
├── PurchaseOrderFiles[] (Entity)
├── AuditLog[] (Value Object)
└── DomainEvents[] (Value Object)
```

**Key Business Rules:**
- Internal orders trigger automatic PDF generation
- External orders require customer PO numbers
- WHT calculations follow Thailand tax regulations
- Status transitions follow strict lifecycle rules
- All operations are audited with tamper-proof logs

## 🛠️ Development Setup

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 14+
- Docker (optional)
- Git

### Environment Variables

**Required for Development:**
```bash
# Database (single connection string)
ConnectionStrings__PurchaseOrderDbContext="Server=localhost;Port=5432;Database=purchaseorder_app_db;Username=postgres;Password=your_password"

# External Services
SUPPLIER_SERVICE_URL=https://dev.api.maliev.com/suppliers
ORDER_SERVICE_URL=https://dev.api.maliev.com/orders
CURRENCY_SERVICE_URL=https://dev.api.maliev.com/currencies
UPLOAD_SERVICE_URL=https://dev.api.maliev.com/uploads
PDF_SERVICE_URL=https://dev.api.maliev.com/pdf

# JWT Authentication
Jwt__SecurityKey=development-secret-key-minimum-256-bits
Jwt__Issuer=maliev-dev
Jwt__Audience=maliev-dev

# CORS
CORS_ALLOWED_ORIGINS=https://dev.intranet.maliev.com,https://dev.api.maliev.com,http://localhost:3000
```

### Database Setup

**Option 1: Local PostgreSQL**
```sql
CREATE DATABASE purchaseorder_app_db;
CREATE USER dev_user WITH PASSWORD 'dev_password';
GRANT ALL PRIVILEGES ON DATABASE purchaseorder_app_db TO dev_user;
```

**Option 2: Docker**
```bash
docker run --name postgres-dev \
  -e POSTGRES_DB=purchaseorder_app_db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=your_password \
  -p 5432:5432 -d postgres:14
```

### Running Locally

```bash
# Apply migrations
dotnet ef database update --project Maliev.PurchaseOrderService.Data

# Run application
dotnet run --project Maliev.PurchaseOrderService.Api --environment Development

# Run tests
dotnet test --verbosity normal
```

**Access Points:**
- API: http://localhost:5278
- Swagger: http://localhost:5278/purchaseorders/swagger
- Health: http://localhost:5278/purchaseorders/liveness

## 🚀 Deployment

### CI/CD with GitHub Actions + GitOps

The service uses a GitOps deployment model with GitHub Actions for CI and ArgoCD for CD.

#### Deployment Flow
```
GitHub Push → GitHub Actions CI → Docker Build → GitOps Repository Update → ArgoCD Sync → Kubernetes Deployment
```

#### Environment-Specific Workflows

**1. Development Environment**
```yaml
# .github/workflows/ci-develop.yml
name: CI - Develop
on:
  push:
    branches: [develop]
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v5
    - uses: actions/setup-dotnet@v5
      with: { dotnet-version: '9.x' }
    - run: dotnet test --verbosity normal
    - name: Build and Push Docker Image
      run: |
        docker build -t asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/purchaseorder-service:${{ github.sha }} .
        docker push asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/purchaseorder-service:${{ github.sha }}
    - name: Update GitOps Repository
      run: |
        cd maliev-gitops/3-apps/purchaseorder-service/overlays/development
        kustomize edit set image purchaseorder-service=asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/purchaseorder-service:${{ github.sha }}
        git commit -am "Update purchaseorder-service to ${{ github.sha }}"
        git push
```

**2. Production Environment**
```yaml
# .github/workflows/ci-main.yml
name: CI - Production
on:
  push:
    branches: [main]
# Similar structure but targets production artifact registry and GitOps overlays
```

#### GitOps Repository Structure
```
maliev-gitops/
├── 3-apps/
│   └── purchaseorder-service/
│       ├── base/
│       │   ├── deployment.yaml
│       │   ├── service.yaml
│       │   ├── configmap.yaml
│       │   └── kustomization.yaml
│       └── overlays/
│           ├── development/
│           ├── staging/
│           └── production/
```

#### Kubernetes Manifests

**Deployment Configuration:**
```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: purchaseorder-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: purchaseorder-service
  template:
    spec:
      containers:
      - name: purchaseorder-service
        image: purchaseorder-service:latest
        ports:
        - containerPort: 8080
        envFrom:
        - secretRef:
            name: maliev-purchaseorder-secrets
        - configMapRef:
            name: maliev-purchaseorder-config
        livenessProbe:
          httpGet:
            path: /purchaseorders/liveness
            port: 8080
        readinessProbe:
          httpGet:
            path: /purchaseorders/readiness
            port: 8080
```

**Environment-Specific Secrets:**
```yaml
# Google Secret Manager → Kubernetes Secrets
apiVersion: v1
kind: Secret
metadata:
  name: maliev-purchaseorder-secrets
data:
  ConnectionStrings__PurchaseOrderDbContext: <base64-encoded>
  Jwt__SecurityKey: <base64-encoded>
  Jwt__Issuer: <base64-encoded>
  Jwt__Audience: <base64-encoded>
  SUPPLIER_SERVICE_URL: <base64-encoded>
  # ... other environment-specific secrets
```

### Environment Configuration

**Development:**
- Database: `purchaseorder_app_db`
- Image Registry: `asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/`
- CORS: `https://dev.intranet.maliev.com`

**Staging:**
- Database: `purchaseorder_staging_db`
- Image Registry: `asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-staging/`
- CORS: `https://staging.intranet.maliev.com`

**Production:**
- Database: `purchaseorder_prod_db`
- Image Registry: `asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-prod/`
- CORS: `https://intranet.maliev.com`

## 📡 API Documentation

### Base URLs
- **Development**: `https://dev.api.maliev.com/purchaseorders`
- **Staging**: `https://staging.api.maliev.com/purchaseorders`
- **Production**: `https://api.maliev.com/purchaseorders`

### Authentication
All endpoints require JWT Bearer token:
```http
Authorization: Bearer <jwt-token>
```

### Core Endpoints

#### Purchase Orders
```http
GET    /v1/purchase-orders              # List with filtering/pagination
POST   /v1/purchase-orders              # Create new purchase order
GET    /v1/purchase-orders/{id}         # Get specific purchase order
PUT    /v1/purchase-orders/{id}         # Update purchase order
DELETE /v1/purchase-orders/{id}         # Soft delete purchase order

POST   /v1/purchase-orders/{id}/approve # Approve purchase order
POST   /v1/purchase-orders/{id}/cancel  # Cancel purchase order
GET    /v1/purchase-orders/stats        # Get statistics
```

#### Order Items
```http
GET    /v1/purchase-orders/{id}/order-items        # Get order items
POST   /v1/purchase-orders/{id}/order-items/refresh # Refresh from OrderService
GET    /v1/purchase-orders/{id}/order-items/summary # Get summary statistics
```

#### Documents
```http
GET    /v1/purchase-orders/{id}/files              # List documents
POST   /v1/purchase-orders/{id}/files              # Upload document
GET    /v1/purchase-orders/{id}/files/{fileId}     # Download document
DELETE /v1/purchase-orders/{id}/files/{fileId}     # Delete document
```

#### WHT Calculations
```http
POST   /v1/purchase-orders/{id}/calculate-wht      # Calculate WHT
GET    /v1/purchase-orders/{id}/wht-history        # Get WHT history
POST   /v1/purchase-orders/{id}/recalculate-wht    # Recalculate WHT
```

### Request/Response Examples

**Create Purchase Order:**
```json
POST /v1/purchase-orders
{
  \"supplierID\": \"SUP-12345\",
  \"orderID\": \"ORD-67890\",
  \"orderType\": \"Internal\",
  \"customerPO\": \"CUST-PO-001\",
  \"description\": \"Office supplies order\",
  \"subtotalAmount\": 10000.00,
  \"currencyCode\": \"THB\",
  \"billingAddress\": {
    \"street\": \"123 Business St\",
    \"city\": \"Bangkok\",
    \"postalCode\": \"10110\",
    \"country\": \"Thailand\"
  }
}
```

**Response:**
```json
{
  \"id\": 1,
  \"orderNumber\": \"PO-2024-000001\",
  \"supplierID\": \"SUP-12345\",
  \"status\": \"Pending\",
  \"orderType\": \"Internal\",
  \"subtotalAmount\": 10000.00,
  \"whtRate\": 3.0,
  \"whtAmount\": 300.00,
  \"totalAmount\": 9700.00,
  \"createdAt\": \"2024-01-15T10:30:00Z\",
  \"createdBy\": \"john.doe@maliev.com\"
}
```

### Error Handling
```json
{
  \"type\": \"https://tools.ietf.org/html/rfc7231#section-6.5.1\",
  \"title\": \"Validation Error\",
  \"status\": 400,
  \"errors\": {
    \"SupplierID\": [\"Supplier ID is required\"],
    \"SubtotalAmount\": [\"Amount must be greater than 0\"]
  }
}
```

## 🧠 Business Logic

### Purchase Order Lifecycle

```
Pending → Approved → Ordered → Delivered
    ↓         ↓         ↓
 Cancelled  Cancelled  Cancelled
```

**Status Transitions:**
- **Pending**: Initial state, awaiting manager approval
- **Approved**: Manager approved, ready for ordering
- **Ordered**: Purchase order sent to supplier
- **Delivered**: Goods/services received and verified
- **Cancelled**: Order cancelled (can happen at any stage)

### WHT (Withholding Tax) Calculations

Thailand tax compliance with automatic WHT calculations:

**Standard Rates:**
- Services: 3%
- Goods: 1%
- Professional Services: 5%
- Advertising: 2%

**Business Rules:**
- WHT applied to all Thai suppliers
- Rates configurable per supplier type
- Automatic recalculation on amount changes
- Full audit trail for tax compliance

### Document Management Workflow

```
Upload → Virus Scan → Store in GCS → Generate Metadata → Index for Search
```

**File Processing:**
1. **Upload Validation**: Size (50MB max), type, virus scan
2. **Storage**: Google Cloud Storage with versioning
3. **Metadata**: Extract and store searchable metadata
4. **Audit**: Log all file operations with user tracking

### PDF Generation (Internal Orders Only)

**Trigger Events:**
- Purchase order approval
- Purchase order update (if already approved)
- Manual regeneration request

**Process:**
1. Domain event published on approval
2. Background service processes event
3. PDF generated via PdfService
4. File stored and linked to purchase order
5. Notification sent to relevant parties

## 🔗 External Integrations

### Service Dependencies

**SupplierService**
- **Purpose**: Supplier validation and address management
- **Pattern**: Synchronous REST API calls with circuit breaker
- **Fallback**: Cached supplier data for read operations

**OrderService**
- **Purpose**: Order items derivation and inventory tracking
- **Pattern**: Cache-aside with background refresh
- **Refresh**: Manual trigger or scheduled updates

**CurrencyService**
- **Purpose**: Currency validation and exchange rates
- **Pattern**: Cache-first with TTL-based expiration
- **Cache Duration**: 1 hour for exchange rates

**UploadService**
- **Purpose**: Document storage in Google Cloud Storage
- **Pattern**: Direct API calls for file operations
- **Features**: Virus scanning, metadata extraction, versioning

**PdfService**
- **Purpose**: PDF generation for internal purchase orders
- **Pattern**: Asynchronous processing via domain events
- **Timeout**: 45 seconds for PDF generation

### Integration Patterns

**Circuit Breaker Pattern:**
```csharp
// Automatic failure handling
services.AddHttpClient<ISupplierService, SupplierService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

**Cache-Aside Pattern:**
```csharp
// Currency service with caching
public async Task<Currency> GetCurrencyAsync(string code)
{
    var cached = await _cache.GetAsync<Currency>($\"currency:{code}\");
    if (cached != null) return cached;

    var currency = await _currencyService.GetAsync(code);
    await _cache.SetAsync($\"currency:{code}\", currency, TimeSpan.FromHours(1));
    return currency;
}
```

## 🔐 Security

### Authentication & Authorization

**JWT Bearer Authentication:**
- Token validation with configurable issuer/audience
- Role-based claims for authorization
- Configurable token expiration and clock skew

**Role-Based Access Control:**
- **Employee**: Read purchase orders, create new orders
- **Manager**: Employee permissions + approval capabilities
- **Procurement**: Manager permissions + advanced procurement features
- **Admin**: Full system access including user management

### Data Protection

**Database Security:**
- Encrypted connections (SSL/TLS)
- Parameterized queries to prevent SQL injection
- Soft deletes for audit trail preservation
- Row-level security for multi-tenant scenarios

**API Security:**
- HTTPS enforcement in production
- CORS configuration for approved origins
- Rate limiting to prevent abuse
- Input validation and sanitization

**Secrets Management:**
- No secrets in source code
- Environment variables for configuration
- Google Secret Manager integration in Kubernetes
- Separate secrets per environment

### Compliance Features

**Audit Trail:**
- Immutable audit logs for all operations
- User tracking for all modifications
- Timestamp precision with UTC normalization
- Tamper-proof log storage

**Data Privacy:**
- GDPR-compliant data handling
- PII encryption at rest
- Data retention policies
- Right to be forgotten implementation

## 📊 Monitoring

### Health Checks

**Liveness Probe:**
```http
GET /purchaseorders/liveness
→ 200 OK "Healthy"
```

**Readiness Probe:**
```http
GET /purchaseorders/readiness
→ 200 OK with detailed health status
```

**Health Check Components:**
- Database connectivity
- External service availability
- Memory usage
- Disk space
- Background service status

### Observability

**Structured Logging:**
```json
{
  \"@t\": \"2024-01-15T10:30:00.000Z\",
  \"@mt\": \"Purchase order {OrderId} approved by {UserId}\",
  \"@l\": \"Information\",
  \"OrderId\": 12345,
  \"UserId\": \"john.doe@maliev.com\",
  \"Application\": \"PurchaseOrderService\"
}
```

**Metrics & Alerting:**
- Business metrics (orders created, approved, cancelled)
- Technical metrics (response times, error rates)
- Infrastructure metrics (CPU, memory, disk)
- Custom alerts for business SLA violations

**Distributed Tracing:**
- Request correlation IDs
- Cross-service trace propagation
- Performance bottleneck identification
- Error trace analysis

### Performance Monitoring

**SLA Targets:**
- API Response Time: p95 < 500ms, p99 < 1s
- Database Query Time: p95 < 100ms
- External Service Calls: p95 < 2s
- PDF Generation: p95 < 30s
- File Upload: p95 < 10s

## 🧪 Testing

### Test Coverage
- **Unit Tests**: Business logic and service layers
- **Integration Tests**: 120+ test methods covering all endpoints
- **Contract Tests**: External service interface validation
- **End-to-End Tests**: Complete workflow validation

### Test Categories

**Business Logic Tests:**
- Purchase order lifecycle validation
- WHT calculation accuracy
- Approval workflow constraints
- Document management workflows

**Integration Tests:**
- API endpoint functionality
- Database operations
- External service integration
- Authentication and authorization

**Performance Tests:**
- Load testing for high-volume scenarios
- Stress testing for system limits
- Endurance testing for memory leaks
- Spike testing for traffic bursts

## 📚 Additional Resources

- [API Swagger Documentation](./swagger.json)
- [Architecture Decision Records](./docs/adr/)
- [Troubleshooting Guide](./docs/troubleshooting.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Follow coding standards and add tests
4. Submit a pull request with detailed description

## 📄 License

Copyright © 2024 Maliev Co. Ltd. All rights reserved.

---

**🚀 Ready to deploy? The service is production-ready with comprehensive CI/CD, monitoring, and security features.**