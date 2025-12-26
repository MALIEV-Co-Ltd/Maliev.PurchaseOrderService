# Maliev Purchase Order Service

Procurement and purchase order management system for the MALIEV platform, handling purchase requisitions, PO creation, approval workflows, and supplier order tracking with full IAM integration.

## Service Description

The Purchase Order Service manages the complete procurement lifecycle from requisition through PO creation, approval, supplier fulfillment, and goods receipt. It integrates with Supplier, Inventory, and Accounting services to provide comprehensive procurement management.

## Architecture Overview

### Project Structure
```
Maliev.PurchaseOrderService/
├── Maliev.PurchaseOrderService.Api/          # Presentation layer
│   ├── Controllers/                          # REST API endpoints
│   ├── Services/                             # Business logic
│   └── Models/                               # DTOs
├── Maliev.PurchaseOrderService.Data/         # Data access layer
│   ├── Entities/                             # EF Core entities
│   └── Migrations/                           # Database migrations
└── Maliev.PurchaseOrderService.Tests/        # Integration tests
```

## Technologies Used

- **.NET 10.0** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL 18** - Relational database
- **Redis** - Distributed caching
- **RabbitMQ** - Message queue via MassTransit
- **OpenTelemetry** - Observability

## Dependencies

### Databases
- **PostgreSQL**: PO headers, line items, receipts, approval workflow
- **Redis**: Caching for supplier data and PO status

### Messaging
- **RabbitMQ**: Events for PO approval, dispatch, receipt

### External Services
- **IAM Service**: Authentication and authorization
- **Supplier Service**: Supplier information and evaluation
- **Material Service**: Material/product master data
- **Inventory Service**: Goods receipt and stock updates
- **Accounting Service**: Invoice matching and payment processing

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

### Predefined Roles
- **Requisitioner**: Create purchase requisitions
- **Buyer**: Create and manage purchase orders
- **Procurement Manager**: Approve purchase orders and manage suppliers
- **Warehouse**: Record goods receipts

## API Endpoints

### Purchase Orders
- `GET /v1/purchase-orders` - List purchase orders
- `POST /v1/purchase-orders` - Create new PO
- `GET /v1/purchase-orders/{id}` - Get PO details
- `PUT /v1/purchase-orders/{id}` - Update draft PO
- `DELETE /v1/purchase-orders/{id}` - Delete draft PO
- `POST /v1/purchase-orders/{id}/submit` - Submit for approval
- `POST /v1/purchase-orders/{id}/approve` - Approve PO
- `POST /v1/purchase-orders/{id}/reject` - Reject PO
- `POST /v1/purchase-orders/{id}/send` - Send to supplier
- `POST /v1/purchase-orders/{id}/cancel` - Cancel PO
- `POST /v1/purchase-orders/{id}/receive` - Record goods receipt
- `GET /v1/purchase-orders/supplier/{supplierId}` - Get supplier POs
- `GET /v1/purchase-orders/{id}/pdf` - Generate PO PDF

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "PurchaseOrderDatabase": "Host=postgres;Port=5432;Database=maliev_purchase_orders;Username=app;Password=secret",
    "Redis": "redis:6379"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Key": "base64-encoded-key",
    "Issuer": "maliev-purchase-order-service",
    "Audience": "maliev-services"
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080"
    }
  },
  "PurchaseOrder": {
    "RequireApprovalAboveAmount": 50000,
    "AutoCloseAfterDays": 90
  }
}
```

## Database

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `PurchaseOrders` - PO headers (supplier, dates, total, status)
- `PurchaseOrderItems` - Line items (material, quantity, price)
- `GoodsReceipts` - Receipt records (date, quantity, quality)
- `PurchaseRequisitions` - Requisition requests
- `POApprovals` - Approval workflow tracking

**Status Values:**
- Draft, Submitted, Approved, Rejected, Sent, PartiallyReceived, Received, Closed, Cancelled

## Running the Service

### Development
```bash
cd Maliev.PurchaseOrderService.Api
dotnet run
```

**Access:**
- API: http://localhost:5000
- Health: http://localhost:5000/purchase-orders/liveness

### Docker
```bash
docker build -t maliev/purchase-order-service:latest .
docker run -p 8080:8080 maliev/purchase-order-service:latest
```

### Tests
```bash
dotnet test
```

## Test Status

**From Test Summary (2025-12-24):**
- **Status**: PASSED (No tests found)
- **Note**: Test infrastructure exists but no tests currently defined

## Key Features

- **Requisition-to-PO Conversion**: Convert approved requisitions to purchase orders
- **Multi-Level Approval**: Configurable approval workflow based on amount
- **Supplier Integration**: Send POs electronically to suppliers
- **Goods Receipt**: Track partial and complete deliveries
- **3-Way Matching**: PO, receipt, and invoice matching
- **Price History**: Track price changes over time
- **Blanket Orders**: Support for recurring purchases
- **Performance Tracking**: Supplier delivery and quality metrics

## Events Published

- `PurchaseOrderCreatedEvent` - New PO created
- `PurchaseOrderApprovedEvent` - PO approved
- `PurchaseOrderSentEvent` - PO sent to supplier
- `GoodsReceivedEvent` - Goods receipt recorded
- `PurchaseOrderClosedEvent` - PO completed and closed

## Support

- Test Summary: `B:\maliev\all-services-test-summary.txt`
- ServiceDefaults: `B:\maliev\Maliev.Aspire\Maliev.Aspire.ServiceDefaults\README.md`

## License

Proprietary - Copyright 2025 MALIEV Co., Ltd. All rights reserved.
