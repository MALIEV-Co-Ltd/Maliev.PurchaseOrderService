# Maliev Purchase Order Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.PurchaseOrderService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Enterprise procurement and purchase order management system for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The primary orchestrator for outbound procurement. It manages the complete lifecycle of purchase requisitions and orders, coordinating with Suppliers, Materials, and Accounting services to ensure professional resource acquisition and 3-way matching.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-speed supplier & material caching)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Procurement Workflow**: End-to-end management from employee-initiated requisitions to formal purchase orders.
- **Multi-Level Approval Engine**: Sophisticated routing for order approvals based on department, amount thresholds, and hierarchical authority.
- **Supplier Integration**: Real-time synchronization with external suppliers for order transmission and shipment tracking.
- **3-Way Matching**: Automated reconciliation between Purchase Orders, Goods Receipts, and Invoices for financial compliance.
- **Comprehensive Lifecycle Status**: Granular tracking from Draft to Sent, Partially Received, and Closed states with complete audit history.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.PurchaseOrderService.git
cd Maliev.PurchaseOrderService
```

2. **Spin up Infrastructure**
```bash
docker run --name po-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name po-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__PurchaseOrderDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.PurchaseOrderService.Data
dotnet run --project Maliev.PurchaseOrderService.Api
```

The service will be available at `http://localhost:5000/purchase-order`. Access the interactive documentation at `http://localhost:5000/purchase-order/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/purchase-order/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/purchase-orders` | Create a new purchase order |
| POST | `/purchase-orders/{id}/submit` | Submit order for administrative approval |
| POST | `/purchase-orders/{id}/receive` | Record a goods receipt against an order |
| GET | `/requisitions` | List and review purchase requisitions |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /purchase-order/liveness`
- **Readiness**: `GET /purchase-order/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /purchase-order/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-purchase-order-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
