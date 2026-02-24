# Research: .NET 10 Microservices Best Practices for PurchaseOrderService

## Executive Summary

This research provides comprehensive guidance for building production-ready .NET 10 WebAPI microservices suitable for a purchase order management system with enterprise-grade requirements including role-based access control, audit trails, optimistic concurrency control, high performance, and Kubernetes deployment.

## 1. .NET 10 WebAPI Project Structure and Configuration

### Decision: Layered Architecture with Standard .NET Project Structure
**Rationale**: .NET 10 provides enhanced microservices features with up to 50% better performance. Following proven layered architecture patterns ensures maintainability and testability.

**Alternatives Considered**: Clean Architecture, Hexagonal Architecture
- **Clean Architecture**: More complex setup, better for larger teams
- **Hexagonal Architecture**: Good for domain-rich applications but adds complexity

### Key Recommendations:
- Use modular monolith approach initially with clear domain boundaries
- Implement route grouping for cleaner endpoint organization
- Leverage .NET 10's enhanced parameter binding for simplified controllers
- Auto-generate OpenAPI documentation for better API discoverability

## 2. Entity Framework Core 10.0 Optimistic Concurrency Control

### Decision: RowVersion/Timestamp Pattern with Automatic Retry Logic
**Rationale**: Database-managed concurrency tokens provide minimal overhead (8 bytes) with reliable conflict detection. EF Core 10.0 provides enhanced performance for PostgreSQL with better query compilation.

**Alternatives Considered**:
- **ConcurrencyCheck Attribute**: Application-managed but requires manual token handling
- **Pessimistic Locking**: Blocks operations, reduces system scalability

### Implementation Pattern:
```csharp
public class PurchaseOrder
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; } // Auto-managed concurrency token
}
```

### Conflict Resolution Strategy:
- **Client Wins**: For non-critical updates
- **Store Wins**: For critical business data
- **Merge Pattern**: Custom field-specific resolution logic

## 3. JWT Bearer Authentication with Role-Based Authorization

### Decision: Centralized JWT Authentication with Role-Based Policies
**Rationale**: JWT provides stateless authentication ideal for microservices. .NET 10's enhanced JWT authentication offers improved performance optimizations.

**Alternatives Considered**:
- **Cookie Authentication**: Not suitable for microservices
- **OAuth 2.0 / OpenID Connect**: More complex but better for larger ecosystems

### Role Hierarchy Implementation:
- **employee**: Create and view own purchase orders
- **manager**: Approve/cancel/reject orders in their department
- **procurement**: Full access to all purchase orders
- **admin**: Override, audit, manage users

### Security Best Practices:
- HTTPS-only communication
- 8-hour token expiry with refresh mechanism
- Secure key storage (Azure Key Vault)
- Rate limiting for token abuse prevention

## 4. Serilog Structured Logging Configuration

### Decision: Serilog with Console/Stdout Output for Container Environments
**Rationale**: .NET 10's runtime optimizations with Serilog deliver up to 50% better logging performance. Stdout-only logging is optimal for Kubernetes log aggregation.

**Alternatives Considered**:
- **Built-in .NET Logging**: Less feature-rich for structured logging
- **NLog**: Good performance but less structured data support
- **File Logging**: Not suitable for container environments

### Configuration Pattern:
```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [{ "Name": "Console" }],
    "Enrich": ["FromLogContext", "WithThreadId"],
    "Properties": { "ServiceName": "PurchaseOrderService" }
  }
}
```

### Audit Trail Integration:
- Log all CRUD operations with user context
- Include before/after values for updates
- Structured properties for correlation IDs
- Performance-optimized async processing

## 5. PostgreSQL Integration and Performance Optimization

### Decision: EF Core 10.0 with Connection Pooling and Query Optimization
**Rationale**: EF Core 10.0 provides significant PostgreSQL performance improvements including better query compilation, improved change tracking, and enhanced connection management.

**Alternatives Considered**:
- **Dapper**: Better performance but more manual mapping
- **ADO.NET**: Maximum control but high development overhead

### Performance Optimization Strategies:

#### Query Patterns:
```csharp
// Projection-based queries with AsNoTracking()
return await _context.PurchaseOrders
    .AsNoTracking()
    .Select(po => new PurchaseOrderSummaryDto { ... })
    .ToListAsync();

// Compiled queries for frequent operations
private static readonly Func<PurchaseOrderContext, string, Task<PurchaseOrder>>
    GetByOrderNumberCompiled = EF.CompileAsyncQuery(...);
```

#### Database Design:
- Composite indexes for frequent query patterns
- Specify precise data types (decimal(18,2) vs nvarchar(max))
- Optimize string lengths for better storage
- Use AsSplitQuery() for complex includes

### Connection Optimization:
- DbContext pooling for high-throughput scenarios (poolSize: 32)
- Connection retry logic with exponential backoff
- Optimized connection string parameters

## 6. Docker Containerization Best Practices

### Decision: Multi-Stage Dockerfile with Security-First Approach
**Rationale**: .NET 10 containerization benefits from multi-stage builds that minimize image size while maintaining security. Non-root user execution is essential for production security.

**Alternatives Considered**:
- **Single-stage builds**: Larger images with build tools
- **Alpine base images**: Smaller but potential compatibility issues
- **Chiseled images**: Most secure but newest option

### Security Implementation:
```dockerfile
# Ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN chown -R app:app /app

# Switch to non-root user
USER app
```

### Health Check Configuration:
```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/purchase-orders/liveness || exit 1
```

### Performance Optimizations:
- Layer caching through strategic COPY ordering
- .dockerignore for build context optimization
- Resource constraints for optimal container performance
- Graceful shutdown handling for zero-downtime deployments

## Architecture Decisions Summary

| Component | Decision | Primary Benefit |
|-----------|----------|-----------------|
| **Framework** | .NET 10 WebAPI | 50% performance improvement, enhanced microservices features |
| **Database Access** | EF Core 10.0 with PostgreSQL | Best-in-class performance, strong typing, migration support |
| **Authentication** | JWT Bearer with role-based policies | Stateless, scalable, microservices-ready |
| **Logging** | Serilog with stdout output | Container-optimized, structured data, high performance |
| **Concurrency** | Optimistic with RowVersion | Minimal overhead, database-managed, scalable |
| **Containerization** | Multi-stage Docker with non-root user | Security-first, minimal image size, production-ready |

## Performance Targets

- **API Response Time**: <200ms p95
- **Throughput**: 1000+ requests/second per instance
- **Memory Usage**: <512MB per container instance
- **Database Connections**: Pooled, max 20 connections per instance
- **Concurrent Operations**: Lock-free with conflict resolution

## Security Implementation

- **Authentication**: JWT tokens with 8-hour expiry
- **Authorization**: Role-based with ownership validation
- **Audit Trail**: All operations logged with user context
- **Container Security**: Non-root execution, vulnerability scanning
- **Data Protection**: Optimistic concurrency, input validation

## Integration Considerations

- **API Gateway**: Token validation and routing
- **Service Discovery**: Kubernetes service mesh integration
- **Monitoring**: Prometheus metrics, distributed tracing
- **Backup Strategy**: PostgreSQL clustering with automated backups
- **CI/CD**: Automated testing, security scanning, zero-downtime deployment

## External Service API Endpoints

All external services use versioned API endpoints for consistency and maintainability:

### Service Endpoint Specifications
- **SupplierService**: `{BASE_URL}/suppliers/v1` - Supplier validation and address management
- **OrderService**: `{BASE_URL}/orders/v1` - Order items derivation (read-only access)
- **CurrencyService**: `{BASE_URL}/currencies/v1` - Currency validation and caching
- **UploadService**: `{BASE_URL}/uploads/v1` - Document management in GCS bucket
- **PdfService**: `{BASE_URL}/pdfs/v1` - Automatic PDF generation for internal POs
- **AuthenticationService**: `{BASE_URL}/auth/v1` - JWT token validation and user authentication

### Environment Configuration Pattern
```csharp
// Environment variables for service discovery (structured configuration)
ExternalServices__SupplierService__BaseUrl=https://dev.api.maliev.com/suppliers
ExternalServices__OrderService__BaseUrl=https://dev.api.maliev.com/orders
ExternalServices__CurrencyService__BaseUrl=https://dev.api.maliev.com/currencies
ExternalServices__UploadService__BaseUrl=https://dev.api.maliev.com/uploads
ExternalServices__PdfService__BaseUrl=https://dev.api.maliev.com/pdfs
ExternalServices__AuthService__BaseUrl=https://dev.api.maliev.com/auth

// Timeout configuration
ExternalServices__SupplierService__TimeoutInSeconds=180
ExternalServices__OrderService__TimeoutInSeconds=180
ExternalServices__CurrencyService__TimeoutInSeconds=180
ExternalServices__UploadService__TimeoutInSeconds=180
ExternalServices__PdfService__TimeoutInSeconds=180
ExternalServices__AuthService__TimeoutInSeconds=180
```

### HttpClient Configuration
- Named clients for each service with specific timeout and retry policies
- `Microsoft.Extensions.Http.Resilience` integration for resilience patterns
- Automatic service discovery through environment-based configuration
- Health check integration for external service availability monitoring

This research forms the foundation for implementing a production-ready PurchaseOrderService microservice that meets all specified requirements for performance, security, scalability, and maintainability.