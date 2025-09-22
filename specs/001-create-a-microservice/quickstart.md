# Quickstart Guide: PurchaseOrderService

## Overview
This quickstart guide demonstrates how to use the PurchaseOrderService API to manage purchase orders. It covers the complete workflow from creating orders to managing their lifecycle through different statuses.

## Prerequisites
- Valid JWT authentication token with appropriate role claims
- Access to the PurchaseOrderService API endpoint
- API client (curl, Postman, or programmatic client)
- Access to external services with versioned endpoints:
  - SupplierService: `/suppliers/v1`
  - OrderService: `/orders/v1`
  - CurrencyService: `/currencies/v1`
  - UploadService: `/uploads/v1`
  - PdfService: `/pdfs/v1`
  - AuthenticationService: `/auth/v1`

## Authentication
All API calls require a JWT Bearer token in the Authorization header:
```http
Authorization: Bearer <your-jwt-token>
```

The token must contain role claims:
- `employee`: Create and view own orders
- `manager`: Approve/cancel orders in department
- `procurement`: Full access to all orders
- `admin`: Override and audit capabilities

## Quick Start Scenarios

### Scenario 1: Employee Creates a Purchase Order Based on Existing Order

#### 1.1 Create a Purchase Order from Customer Quotation
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierID": 1234,
  "orderID": 5678,
  "currencyID": 1,
  "orderType": "External",
  "customerPO": "CUST-PO-2025-5678",
  "expectedDeliveryDate": "2025-10-01T00:00:00Z",
  "whtRate": 3.0,
  "notes": "Urgent order for customer project - expedite delivery",
  "shippingAddress": {
    "addressType": "Shipping",
    "contactName": "John Manufacturing",
    "addressLine1": "123 Factory Street",
    "city": "Industrial City",
    "postalCode": "12345",
    "country": "USA",
    "phoneNumber": "+1-555-0123",
    "emailAddress": "receiving@maliev.com"
  }
}
```

**Response:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "customerPO": "CUST-PO-2025-5678",
  "supplierID": 1234,
  "supplierName": "TechParts Supply Co.",
  "orderID": 5678,
  "currencyID": 1,
  "currencyCode": "USD",
  "currencySymbol": "$",
  "orderDate": "2025-09-18T10:30:00Z",
  "expectedDeliveryDate": "2025-10-01T00:00:00Z",
  "status": "Pending",
  "orderType": "External",
  "subtotalAmount": 198.75,
  "whtRate": 3.0,
  "whtAmount": 5.96,
  "totalAmount": 192.79,
  "currency": "USD",
  "createdBy": "emp_12345",
  "createdAt": "2025-09-18T10:30:00Z",
  "lastModifiedBy": null,
  "lastModifiedAt": null,
  "approvedBy": null,
  "approvedAt": null,
  "notes": "Urgent order for customer project - expedite delivery",
  "rowVersion": "AAAAAAAAB9E="
}
```

#### 1.2 View Created Order
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/12345
Authorization: Bearer <employee-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "customerPO": "CUST-PO-2025-5678",
  "supplierID": 1234,
  "supplierName": "TechParts Supply Co.",
  "orderID": 5678,
  "currencyID": 1,
  "currencyCode": "USD",
  "currencySymbol": "$",
  "status": "Pending",
  "totalAmount": 198.75,
  "items": [
    {
      "id": 67890,
      "externalOrderItemId": 9876,
      "productCode": "BEARING-001",
      "productName": "Industrial Ball Bearing 25mm",
      "quantity": 10,
      "unitOfMeasure": "pcs",
      "unitPrice": 15.50,
      "totalPrice": 155.00,
      "currency": "USD",
      "deliveryDate": "2025-10-01T00:00:00Z",
      "notes": "Critical component - expedite if possible",
      "cachedAt": "2025-09-18T10:30:00Z",
      "externallyModified": false
    },
    {
      "id": 67891,
      "externalOrderItemId": 9877,
      "productCode": "SEAL-045",
      "productName": "Hydraulic Seal Ring",
      "quantity": 5,
      "unitOfMeasure": "pcs",
      "unitPrice": 8.75,
      "totalPrice": 43.75,
      "currency": "USD",
      "notes": "Backup seals for maintenance",
      "cachedAt": "2025-09-18T10:30:00Z",
      "externallyModified": false
    }
  ],
  "shippingAddress": {
    "id": 4567,
    "addressType": "Shipping",
    "contactName": "John Manufacturing",
    "addressLine1": "123 Factory Street",
    "city": "Industrial City",
    "postalCode": "12345",
    "country": "USA",
    "phoneNumber": "+1-555-0123",
    "emailAddress": "receiving@maliev.com"
  }
}
```

### Scenario 2: Manager Approves the Purchase Order

#### 2.1 Manager Reviews Orders in Department
**Request:**
```http
GET /purchase-orders?status=Pending&page=1&pageSize=20
Authorization: Bearer <manager-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "items": [
    {
      "id": 12345,
      "orderNumber": "PO-2025-001234",
      "supplierName": "TechParts Supply Co.",
      "status": "Pending",
      "totalAmount": 198.75,
      "createdBy": "emp_12345",
      "createdAt": "2025-09-18T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1,
    "hasNextPage": false,
    "hasPreviousPage": false
  }
}
```

#### 2.2 Approve the Purchase Order
**Request:**
```http
POST /purchaseorders/v1/purchase-orders/12345/approve
Content-Type: application/json
Authorization: Bearer <manager-token>

{
  "notes": "Approved for urgent production needs. Expedite delivery."
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "status": "Approved",
  "approvedBy": "mgr_67890",
  "approvedAt": "2025-09-18T11:15:00Z",
  "notes": "Approved for urgent production needs. Expedite delivery.",
  "rowVersion": "AAAAAAAAB9F="
}
```

### Scenario 3: Update Purchase Order (Optimistic Concurrency)

#### 3.1 Update Order with Concurrency Control
**Request:**
```http
PUT /purchaseorders/v1/purchase-orders/12345
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierName": "TechParts Supply Co.",
  "expectedDeliveryDate": "2025-09-28T00:00:00Z",
  "notes": "URGENT: Delivery moved up due to critical maintenance",
  "rowVersion": "AAAAAAAAB9F="
}
```

**Successful Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "expectedDeliveryDate": "2025-09-28T00:00:00Z",
  "notes": "URGENT: Delivery moved up due to critical maintenance",
  "lastModifiedBy": "emp_12345",
  "lastModifiedAt": "2025-09-18T11:30:00Z",
  "rowVersion": "AAAAAAAAB9G="
}
```

#### 3.2 Concurrency Conflict Example
**Request with Outdated RowVersion:**
```http
PUT /purchaseorders/v1/purchase-orders/12345
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierName": "Different Supplier",
  "rowVersion": "AAAAAAAAB9E="
}
```

**Conflict Response:**
```http
HTTP/1.1 409 Conflict
Content-Type: application/json

{
  "error": {
    "code": "ConcurrencyConflict",
    "message": "The record has been modified by another user. Please refresh and try again.",
    "details": [
      {
        "field": "rowVersion",
        "message": "The provided version token is outdated"
      }
    ],
    "traceId": "trace-12345-67890"
  }
}
```

### Scenario 4: Search and Filter Orders

#### 4.1 Search Orders by Supplier
**Request:**
```http
GET /purchase-orders?supplierID=1234&status=Approved&sortBy=createdAt&sortDirection=desc
Authorization: Bearer <procurement-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "items": [
    {
      "id": 12345,
      "orderNumber": "PO-2025-001234",
      "supplierName": "TechParts Supply Co.",
      "status": "Approved",
      "totalAmount": 198.75,
      "createdAt": "2025-09-18T10:30:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 1,
    "totalPages": 1
  }
}
```

#### 4.2 Date Range Filtering
**Request:**
```http
GET /purchase-orders?createdFrom=2025-09-01T00:00:00Z&createdTo=2025-09-30T23:59:59Z&orderType=Internal
Authorization: Bearer <procurement-token>
```

### Scenario 5: Handle External Service Integration

#### 5.1 Refresh Order Items from External Service
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/12345/items?refresh=true
Authorization: Bearer <employee-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": 67890,
    "externalOrderItemId": 9876,
    "productCode": "BEARING-001",
    "productName": "Industrial Ball Bearing 25mm",
    "quantity": 10,
    "unitOfMeasure": "pcs",
    "unitPrice": 15.50,
    "totalPrice": 155.00,
    "currency": "USD",
    "deliveryDate": "2025-10-01T00:00:00Z",
    "notes": "Critical component - expedite if possible",
    "cachedAt": "2025-09-18T15:00:00Z",
    "externallyModified": false
  }
]
```

#### 5.2 Handle External Service Validation Errors
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierID": 9999,
  "orderID": 5678,
  "orderType": "External"
}
```

**Response (Invalid SupplierID):**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": {
    "code": "ValidationFailed",
    "message": "External service validation failed",
    "details": [
      {
        "field": "supplierID",
        "message": "Supplier with ID 9999 not found in SupplierService"
      }
    ],
    "traceId": "trace-12345-67890"
  }
}
```

### Scenario 6: Withholding Tax (WHT) Management

#### 6.1 Update Purchase Order with WHT
**Request:**
```http
PUT /purchaseorders/v1/purchase-orders/12345
Content-Type: application/json
Authorization: Bearer <procurement-token>

{
  "whtRate": 5.0,
  "notes": "Applied 5% WHT as per tax regulations",
  "rowVersion": "AAAAAAAAB9F="
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "subtotalAmount": 198.75,
  "whtRate": 5.0,
  "whtAmount": 9.94,
  "totalAmount": 188.81,
  "lastModifiedBy": "proc_12345",
  "lastModifiedAt": "2025-09-18T12:30:00Z",
  "rowVersion": "AAAAAAAAB9H="
}
```

#### 6.2 WHT Validation Error Example
**Request:**
```http
PUT /purchaseorders/v1/purchase-orders/12345
Content-Type: application/json
Authorization: Bearer <procurement-token>

{
  "whtRate": 25.0,
  "rowVersion": "AAAAAAAAB9H="
}
```

**Response (WHT Rate Exceeds Legal Limit):**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": {
    "code": "ValidationFailed",
    "message": "WHT rate exceeds legal limits",
    "details": [
      {
        "field": "whtRate",
        "message": "WHT rate cannot exceed 15% as per tax regulations"
      }
    ],
    "traceId": "trace-12345-67890"
  }
}
```

### Scenario 7: Cancel Purchase Order

#### 7.1 Cancel Order with Reason
**Request:**
```http
POST /purchaseorders/v1/purchase-orders/12345/cancel
Content-Type: application/json
Authorization: Bearer <manager-token>

{
  "reason": "Project requirements changed - equipment no longer needed"
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12345,
  "orderNumber": "PO-2025-001234",
  "status": "Cancelled",
  "notes": "Project requirements changed - equipment no longer needed",
  "lastModifiedBy": "mgr_67890",
  "lastModifiedAt": "2025-09-18T14:30:00Z",
  "rowVersion": "AAAAAAAAB9H="
}
```

### Scenario 8: Currency Management and CurrencyService Integration

#### 8.1 Create Purchase Order with Different Currency (THB)
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierID": 1234,
  "orderID": 9012,
  "currencyID": 2,
  "orderType": "External",
  "expectedDeliveryDate": "2025-10-15T00:00:00Z",
  "whtRate": 3.0,
  "notes": "Thai supplier for local manufacturing project",
  "shippingAddress": {
    "addressType": "Shipping",
    "contactName": "Bangkok Manufacturing Hub",
    "addressLine1": "456 Industrial Road",
    "city": "Bangkok",
    "postalCode": "10330",
    "country": "Thailand",
    "phoneNumber": "+66-2-555-0123",
    "emailAddress": "receiving@maliev.co.th"
  }
}
```

**Response:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": 12349,
  "orderNumber": "PO-2025-001238",
  "supplierID": 1234,
  "supplierName": "Bangkok Parts Ltd.",
  "orderID": 9012,
  "currencyID": 2,
  "currencyCode": "THB",
  "currencySymbol": "฿",
  "orderDate": "2025-09-18T12:00:00Z",
  "expectedDeliveryDate": "2025-10-15T00:00:00Z",
  "status": "Pending",
  "orderType": "External",
  "subtotalAmount": 6750.00,
  "whtRate": 3.0,
  "whtAmount": 202.50,
  "totalAmount": 6547.50,
  "currency": "THB",
  "createdBy": "emp_12345",
  "createdAt": "2025-09-18T12:00:00Z",
  "notes": "Thai supplier for local manufacturing project",
  "rowVersion": "AAAAAAAAB9F="
}
```

#### 8.2 Update Purchase Order Currency (Triggers Recalculation)
**Request:**
```http
PUT /purchaseorders/v1/purchase-orders/12349
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "currencyID": 1,
  "expectedDeliveryDate": "2025-10-15T00:00:00Z",
  "whtRate": 3.0,
  "notes": "Currency changed to USD due to supplier payment preference",
  "rowVersion": "AAAAAAAAB9F="
}
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": 12349,
  "orderNumber": "PO-2025-001238",
  "supplierID": 1234,
  "supplierName": "Bangkok Parts Ltd.",
  "orderID": 9012,
  "currencyID": 1,
  "currencyCode": "USD",
  "currencySymbol": "$",
  "expectedDeliveryDate": "2025-10-15T00:00:00Z",
  "status": "Pending",
  "orderType": "External",
  "subtotalAmount": 200.00,
  "whtRate": 3.0,
  "whtAmount": 6.00,
  "totalAmount": 194.00,
  "currency": "USD",
  "lastModifiedBy": "emp_12345",
  "lastModifiedAt": "2025-09-18T12:15:00Z",
  "notes": "Currency changed to USD due to supplier payment preference",
  "rowVersion": "AAAAAAAAB9G="
}
```

#### 8.3 Handle Invalid CurrencyID Error
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierID": 1234,
  "orderID": 5678,
  "currencyID": 999,
  "orderType": "External"
}
```

**Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "type": "ValidationError",
  "title": "Invalid Currency ID",
  "status": 400,
  "detail": "The specified CurrencyID (999) does not exist in CurrencyService",
  "instance": "/purchase-orders",
  "errors": {
    "currencyID": [
      "Currency with ID 999 not found in CurrencyService"
    ]
  },
  "traceId": "80000001-0004-ff00-b63f-84710c7967bb"
}
```

#### 8.4 Handle CurrencyService Unavailable Error
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierID": 1234,
  "orderID": 5678,
  "currencyID": 1,
  "orderType": "External"
}
```

**Response:**
```http
HTTP/1.1 502 Bad Gateway
Content-Type: application/json

{
  "type": "ExternalServiceError",
  "title": "External Service Unavailable",
  "status": 502,
  "detail": "CurrencyService is currently unavailable. Please try again later.",
  "instance": "/purchase-orders",
  "service": "CurrencyService",
  "retryAfter": "30 seconds",
  "traceId": "80000001-0004-ff00-b63f-84710c7967bc"
}
```

### Scenario 9: Purchase Order Document Management (Including Automatic PDF Generation)

**Note**: When a purchase order is created or updated, the system automatically generates a PDF document via PdfService and uploads it to UploadService. This generated PDF appears in the documents list with `documentType: "GeneratedPDF"` and `uploadedBy: "system"`. Users cannot upload GeneratedPDF documents manually.

#### 9.1 Upload Customer PO Document
**Request:**
```http
POST /purchaseorders/v1/purchase-orders/12345/files
Content-Type: multipart/form-data
Authorization: Bearer <employee-token>

--boundary
Content-Disposition: form-data; name="file"; filename="customer-po-2025-5678.pdf"
Content-Type: application/pdf

[Binary file content]
--boundary
Content-Disposition: form-data; name="documentType"

CustomerPO
--boundary
Content-Disposition: form-data; name="description"

Customer purchase order document received via email
--boundary--
```

**Response:**
```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "id": 501,
  "purchaseOrderId": 12345,
  "fileName": "customer-po-2025-5678.pdf",
  "objectName": "purchase-orders/12345/documents/customer-po-2025-5678-501.pdf",
  "documentType": "CustomerPO",
  "fileSize": 245760,
  "contentType": "application/pdf",
  "uploadedBy": "emp_12345",
  "uploadedAt": "2025-09-18T15:30:00Z",
  "description": "Customer purchase order document received via email"
}
```

#### 9.2 Get All Documents for Purchase Order
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/12345/files
Authorization: Bearer <employee-token>
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "id": 500,
    "purchaseOrderId": 12345,
    "fileName": "PO-2025-12345-Generated.pdf",
    "objectName": "purchase-orders/12345/generated/PO-2025-12345-Generated.pdf",
    "documentType": "GeneratedPDF",
    "fileSize": 187392,
    "contentType": "application/pdf",
    "uploadedBy": "system",
    "uploadedAt": "2025-09-18T15:00:00Z",
    "description": "Automatically generated purchase order PDF"
  },
  {
    "id": 501,
    "purchaseOrderId": 12345,
    "fileName": "customer-po-2025-5678.pdf",
    "objectName": "purchase-orders/12345/documents/customer-po-2025-5678-501.pdf",
    "documentType": "CustomerPO",
    "fileSize": 245760,
    "contentType": "application/pdf",
    "uploadedBy": "emp_12345",
    "uploadedAt": "2025-09-18T15:30:00Z",
    "description": "Customer purchase order document received via email"
  },
  {
    "id": 502,
    "purchaseOrderId": 12345,
    "fileName": "internal-approval-form.pdf",
    "objectName": "purchase-orders/12345/documents/internal-approval-form-502.pdf",
    "documentType": "InternalApproval",
    "fileSize": 156432,
    "contentType": "application/pdf",
    "uploadedBy": "mgr_67890",
    "uploadedAt": "2025-09-18T16:00:00Z",
    "description": "Approved by department manager"
  }
]
```

#### 9.3 Download Document
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/12345/files/501
Authorization: Bearer <employee-token>
```

**Response:**
```http
HTTP/1.1 302 Found
Location: https://storage.googleapis.com/maliev-uploads/purchase-orders/12345/documents/customer-po-2025-5678-501.pdf?signed_url_params
Content-Type: application/json
```

#### 9.4 Delete Document
**Request:**
```http
DELETE /purchase-orders/12345/files/502
Authorization: Bearer <manager-token>
```

**Response:**
```http
HTTP/1.1 204 No Content
```

#### 9.5 Handle File Upload Error (File Too Large)
**Request:**
```http
POST /purchaseorders/v1/purchase-orders/12345/files
Content-Type: multipart/form-data
Authorization: Bearer <employee-token>

[Large file > 50MB]
```

**Response:**
```http
HTTP/1.1 413 Request Entity Too Large
Content-Type: application/json

{
  "type": "FileTooLarge",
  "title": "File Size Exceeds Limit",
  "status": 413,
  "detail": "The uploaded file size (52.5MB) exceeds the maximum allowed size of 50MB",
  "instance": "/purchase-orders/12345/files",
  "maxFileSize": "50MB",
  "actualFileSize": "52.5MB",
  "traceId": "80000001-0004-ff00-b63f-84710c7967bd"
}
```

#### 9.6 Handle UploadService Unavailable Error
**Request:**
```http
POST /purchaseorders/v1/purchase-orders/12345/files
Content-Type: multipart/form-data
Authorization: Bearer <employee-token>

[Valid file upload request]
```

**Response:**
```http
HTTP/1.1 502 Bad Gateway
Content-Type: application/json

{
  "type": "ExternalServiceError",
  "title": "Upload Service Unavailable",
  "status": 502,
  "detail": "UploadService is currently unavailable. Please try again later.",
  "instance": "/purchase-orders/12345/files",
  "service": "UploadService",
  "retryAfter": "30 seconds",
  "traceId": "80000001-0004-ff00-b63f-84710c7967be"
}
```

## Error Handling Examples

### Validation Error
**Request:**
```http
POST /purchaseorders/v1/purchase-orders
Content-Type: application/json
Authorization: Bearer <employee-token>

{
  "supplierName": "",
  "items": []
}
```

**Response:**
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": {
    "code": "ValidationFailed",
    "message": "Request validation failed",
    "details": [
      {
        "field": "supplierName",
        "message": "Supplier name is required"
      },
      {
        "field": "items",
        "message": "At least one item is required"
      }
    ],
    "traceId": "trace-12345-67890"
  }
}
```

### Authorization Error
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/99999
Authorization: Bearer <employee-token>
```

**Response:**
```http
HTTP/1.1 403 Forbidden
Content-Type: application/json

{
  "error": {
    "code": "InsufficientPermissions",
    "message": "You don't have permission to access this purchase order",
    "traceId": "trace-12345-67890"
  }
}
```

### Resource Not Found
**Request:**
```http
GET /purchaseorders/v1/purchase-orders/99999
Authorization: Bearer <admin-token>
```

**Response:**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "error": {
    "code": "ResourceNotFound",
    "message": "Purchase order with ID 99999 was not found",
    "traceId": "trace-12345-67890"
  }
}
```

## Health Checks

### Liveness Check
**Request:**
```http
GET /health/liveness
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

### Readiness Check
**Request:**
```http
GET /health/readiness
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL connection successful",
      "duration": "00:00:00.045"
    },
    "self": {
      "status": "Healthy",
      "description": "Application is running",
      "duration": "00:00:00.001"
    }
  }
}
```

## External Service Integration Details

The PurchaseOrderService integrates with multiple external services using versioned APIs. Each service provides specific functionality:

### Service Integration Overview

| Service | Endpoint | Purpose | Integration Type |
|---------|----------|---------|-----------------|
| **SupplierService** | `/suppliers/v1` | Supplier validation, address management | Synchronous validation |
| **OrderService** | `/orders/v1` | Order items derivation | Read-only data retrieval |
| **CurrencyService** | `/currencies/v1` | Currency validation, rates | Caching with validation |
| **UploadService** | `/uploads/v1` | Document management | File operations |
| **PdfService** | `/pdfs/v1` | PDF generation | Event-driven async |
| **AuthenticationService** | `/auth/v1` | JWT validation | Security integration |

### Service Interaction Examples

#### SupplierService Integration
When creating a purchase order, the system validates the supplier:
```
1. User provides SupplierID: 1234
2. System calls: GET {SUPPLIER_SERVICE_URL}/suppliers/v1/suppliers/1234
3. If valid, supplier data is cached locally
4. If invalid, 400 Bad Request returned to user
```

#### OrderService Integration
Order items are derived from existing orders/quotations:
```
1. User provides OrderID: 5678
2. System calls: GET {ExternalServices__OrderService__BaseUrl}/v1/orders/5678/items
3. Items are cached locally as read-only data
4. PO creation proceeds with derived items
```

#### CurrencyService Integration
Currency validation and rate information:
```
1. User provides CurrencyID: 1
2. System calls: GET {CURRENCY_SERVICE_URL}/currencies/v1/currencies/1
3. Currency data cached with TTL
4. Exchange rates used for calculations
```

#### UploadService Integration
Document management operations:
```
1. User uploads file via PurchaseOrderService
2. System forwards to: POST {UPLOAD_SERVICE_URL}/uploads/v1/files
3. GCS object path returned and stored in PO record
4. Download requests return signed URLs
```

#### PdfService Integration (Event-Driven)
Automatic PDF generation for internal POs:
```
1. Internal PO created/updated
2. Domain event published: PurchaseOrderCreated/Updated
3. Background service calls: POST {PDF_SERVICE_URL}/pdfs/v1/generate
4. Generated PDF uploaded to UploadService
5. PDF metadata stored in PO record
```

### Environment Configuration

All service endpoints are configured via structured environment variables:
```bash
ExternalServices__SupplierService__BaseUrl=https://api.maliev.com/suppliers
ExternalServices__OrderService__BaseUrl=https://api.maliev.com/orders
ExternalServices__CurrencyService__BaseUrl=https://api.maliev.com/currencies
ExternalServices__UploadService__BaseUrl=https://api.maliev.com/uploads
ExternalServices__PdfService__BaseUrl=https://api.maliev.com/pdfs
ExternalServices__AuthService__BaseUrl=https://api.maliev.com/auth

# Timeout configuration
ExternalServices__SupplierService__TimeoutInSeconds=180
ExternalServices__OrderService__TimeoutInSeconds=180
ExternalServices__CurrencyService__TimeoutInSeconds=180
ExternalServices__UploadService__TimeoutInSeconds=180
ExternalServices__PdfService__TimeoutInSeconds=180
ExternalServices__AuthService__TimeoutInSeconds=180
```

### Resilience Patterns

Each external service integration includes:
- **Circuit Breaker**: Prevent cascade failures
- **Retry Policy**: Handle transient failures
- **Timeout Configuration**: Prevent hanging requests
- **Health Checks**: Monitor service availability
- **Fallback Strategies**: Graceful degradation when possible

## Best Practices for API Usage

### 1. Authentication Token Management
- Always include the Authorization header with Bearer token
- Handle 401 responses by refreshing the token
- Store tokens securely (never in local storage for web apps)

### 2. Concurrency Control
- Always include the `rowVersion` when updating entities
- Handle 409 conflicts by refreshing data and allowing user to resolve conflicts
- Implement retry logic for transient concurrency conflicts

### 3. Error Handling
- Parse error responses to extract specific validation messages
- Use `traceId` for debugging and support requests
- Implement proper logging of API errors on the client side

### 4. Pagination
- Use appropriate page sizes (recommended: 20-50 items)
- Implement proper pagination UI with total count information
- Cache results when appropriate to reduce API calls

### 5. Performance Optimization
- Use filtering parameters to reduce data transfer
- Implement client-side caching for reference data (addresses, suppliers)
- Use compression (Accept-Encoding: gzip) for large responses

### 6. Security
- Validate SSL certificates in production
- Never log or expose authentication tokens
- Implement proper CORS handling for web clients
- Use HTTPS for all API communication

## Integration Testing Scenarios

The quickstart scenarios above can be automated as integration tests to verify:

1. **Complete Order Lifecycle**: Create → View → Update → Approve → Cancel
2. **Role-Based Access Control**: Verify employee can't approve orders, managers can't access other departments' orders
3. **Concurrency Handling**: Simulate concurrent updates and verify conflict resolution
4. **Validation**: Test all required fields and format validations
5. **Error Handling**: Verify proper error responses and status codes
6. **Performance**: Measure response times under load
7. **Health Monitoring**: Verify health endpoints return correct status

This quickstart guide provides a comprehensive foundation for using the PurchaseOrderService API effectively and securely.