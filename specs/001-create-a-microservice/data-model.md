# Data Model: PurchaseOrderService

## Entity Overview

The PurchaseOrderService manages six core entities with clear relationships and business rules derived from the functional requirements, including event-driven PDF generation capabilities.

## Core Entities

### 1. PurchaseOrder (Aggregate Root)
**Purpose**: Represents a request to purchase goods or services from a supplier, linked to external orders and suppliers.

**Attributes**:
- `Id` (int, Primary Key): Unique identifier
- `OrderNumber` (string, 20 chars, Unique): Auto-generated unique internal order number (e.g., "PO-2025-001234")
- `CustomerPO` (string, 50 chars, nullable): Optional customer purchase order number for external POs
- `SupplierID` (int, Foreign Key, required): References a supplier in SupplierService
- `OrderID` (int, Foreign Key, required): References an order/quotation/internal order in OrderService
- `CurrencyID` (int, Foreign Key, required): References a currency in CurrencyService
- `SupplierName` (string, 100 chars, cached): Cached supplier name from SupplierService
- `SupplierContactInfo` (string, 200 chars, nullable, cached): Cached contact information
- `CurrencyCode` (string, 3 chars, cached): Cached currency code from CurrencyService (e.g., "THB", "USD")
- `CurrencySymbol` (string, 10 chars, cached): Cached currency symbol for display
- `OrderDate` (DateTime): Date when the purchase order was created
- `ExpectedDeliveryDate` (DateTime, nullable): Expected delivery date
- `Status` (enum): Order status (Pending, Approved, Ordered, Delivered, Cancelled)
- `OrderType` (enum): Internal (company operations) or External (client projects)
- `SubtotalAmount` (decimal(18,2)): Subtotal amount before WHT calculated from derived order items
- `WHTRate` (decimal(5,2), nullable): Withholding tax rate percentage (0.00-99.99%)
- `WHTAmount` (decimal(18,2), nullable): Calculated withholding tax amount
- `TotalAmount` (decimal(18,2)): Final total amount after WHT deduction (SubtotalAmount - WHTAmount)
- `Currency` (string, 3 chars): Currency code (e.g., "USD", "EUR")
- `CreatedBy` (string, 50 chars): User ID who created the order
- `CreatedAt` (DateTime): Creation timestamp
- `LastModifiedBy` (string, 50 chars, nullable): User ID who last modified the order
- `LastModifiedAt` (DateTime, nullable): Last modification timestamp
- `ApprovedBy` (string, 50 chars, nullable): User ID who approved the order
- `ApprovedAt` (DateTime, nullable): Approval timestamp
- `Notes` (string, 1000 chars, nullable): Additional notes or comments
- `RowVersion` (byte[], Timestamp): Optimistic concurrency control token
- `IsDeleted` (bool): Soft delete flag

**Business Rules**:
- OrderNumber must be unique and auto-generated
- SupplierID must be valid in SupplierService (validated on creation)
- OrderID must be valid and not already linked to another PO (validated on creation)
- CurrencyID must be valid in CurrencyService (validated on creation)
- Items are read-only and derived from referenced OrderService/QuotationService
- Status transitions: Pending → Approved → Ordered → Delivered
- Cancelled status can be set from Pending or Approved
- SubtotalAmount is calculated from derived order items (cached)
- WHTRate must be between 0.00% and legal limit (validation required)
- WHTAmount = SubtotalAmount × (WHTRate / 100) when WHTRate is specified
- TotalAmount = SubtotalAmount - WHTAmount (or SubtotalAmount if no WHT)
- Only users with appropriate roles can modify orders and WHT settings
- Audit trail must be maintained for all changes including WHT calculations
- Cached supplier and currency data is updated when accessed but PO is not dependent on real-time external service data

**Relationships**:
- One-to-Many with OrderItem (read-only, derived from external services)
- One-to-One with Address (ShippingAddress)
- One-to-One with Address (BillingAddress)
- External References: SupplierService (via SupplierID), OrderService (via OrderID), CurrencyService (via CurrencyID)

### 2. OrderItem (Read-Only, Derived Entity)
**Purpose**: Individual line items derived from OrderService/QuotationService for the referenced order.

**Attributes** (All read-only, populated from external services):
- `Id` (int, Primary Key): Local unique identifier for caching
- `PurchaseOrderId` (int, Foreign Key): Reference to parent PurchaseOrder
- `ExternalOrderItemId` (int): Reference to the original item in OrderService
- `ProductCode` (string, 50 chars, nullable): Product/SKU code from external service
- `ProductName` (string, 200 chars): Name/description from external service
- `Quantity` (decimal(10,3)): Quantity from external order
- `UnitOfMeasure` (string, 20 chars): Unit of measure from external service
- `UnitPrice` (decimal(18,2)): Price per unit from external service
- `TotalPrice` (decimal(18,2)): Calculated total (Quantity × UnitPrice)
- `Currency` (string, 3 chars): Currency code (inherited from PurchaseOrder)
- `DeliveryDate` (DateTime, nullable): Expected delivery date for this item
- `Notes` (string, 500 chars, nullable): Item-specific notes from external service
- `CachedAt` (DateTime): When this data was last retrieved from external service
- `ExternallyModified` (bool): Flag indicating if external item has been modified since caching

**Business Rules**:
- All data is derived from OrderService/QuotationService and cached locally
- Items cannot be modified through PurchaseOrderService
- TotalPrice = Quantity × UnitPrice (calculated from external data)
- Data is refreshed when PurchaseOrder is accessed and cache is stale
- External service modifications after PO creation are flagged but don't prevent PO operations
- Currency must match parent PurchaseOrder currency

**Relationships**:
- Many-to-One with PurchaseOrder (required)
- External Reference: OrderService/QuotationService (via ExternalOrderItemId)

### 3. Address
**Purpose**: Shipping and billing addresses for purchase orders.

**Attributes**:
- `Id` (int, Primary Key): Unique identifier
- `AddressType` (enum): Shipping or Billing
- `CompanyName` (string, 100 chars, nullable): Company name
- `ContactName` (string, 100 chars): Contact person name
- `AddressLine1` (string, 100 chars): Primary address line
- `AddressLine2` (string, 100 chars, nullable): Secondary address line
- `City` (string, 50 chars): City name
- `StateProvince` (string, 50 chars, nullable): State or province
- `PostalCode` (string, 20 chars): Postal/ZIP code
- `Country` (string, 50 chars): Country name
- `PhoneNumber` (string, 20 chars, nullable): Contact phone number
- `EmailAddress` (string, 100 chars, nullable): Contact email address
- `CreatedAt` (DateTime): Creation timestamp
- `LastModifiedAt` (DateTime, nullable): Last modification timestamp

**Business Rules**:
- ContactName, AddressLine1, City, PostalCode, and Country are required
- EmailAddress must be valid email format if provided
- PhoneNumber format validation based on country

**Relationships**:
- Many-to-One with PurchaseOrder (ShippingAddressId, BillingAddressId)

### 4. PurchaseOrderFile
**Purpose**: Stores references to documents uploaded to UploadService for purchase orders.

**Attributes**:
- `Id` (int, Primary Key): Unique identifier
- `PurchaseOrderId` (int, Foreign Key): Reference to parent PurchaseOrder
- `FileName` (string, 255 chars): Original filename of the uploaded document
- `ObjectName` (string, 500 chars): Full object path in GCS bucket (for organized storage)
- `DocumentType` (enum): Type of document (CustomerPO, InternalApproval, Invoice, Reference, GeneratedPDF, Other)
- `FileSize` (long): Size of the file in bytes
- `ContentType` (string, 100 chars): MIME type of the file
- `UploadedBy` (string, 50 chars): User ID who uploaded the document
- `UploadedAt` (DateTime): Upload timestamp
- `Description` (string, 500 chars, nullable): Optional description of the document
- `IsDeleted` (bool): Soft delete flag

**Business Rules**:
- PurchaseOrderId must reference a valid PurchaseOrder
- FileName and ObjectName are required
- ObjectName must be unique within the GCS bucket
- FileSize must be greater than 0
- Only authorized users with PO access can upload/download documents
- Documents are stored in UploadService with maliev GCS bucket
- Soft delete maintains audit trail of document management
- DocumentType helps categorize and organize files, including automatically generated PDFs
- GeneratedPDF documents are created automatically when PO is created or updated via PdfService

**Relationships**:
- Many-to-One with PurchaseOrder (required)
- External Reference: UploadService (via ObjectName)

### 5. AuditLog
**Purpose**: Tracks all changes made to purchase orders and external service interactions for compliance and audit trail.

**Attributes**:
- `Id` (long, Primary Key): Unique identifier
- `EntityType` (string, 50 chars): Entity type (e.g., "PurchaseOrder", "OrderItem", "ExternalServiceCall")
- `EntityId` (string, 50 chars): ID of the affected entity
- `Action` (enum): Type of action (Create, Update, Delete, Approve, Cancel, ExternalFetch, ExternalValidation, PDFGenerated, EventPublished)
- `UserId` (string, 50 chars): User who performed the action
- `UserRole` (string, 20 chars): User's role at time of action
- `Timestamp` (DateTime): When the action occurred
- `OldValues` (string, JSON): Previous values (for updates)
- `NewValues` (string, JSON): New values (for creates/updates)
- `ExternalServiceName` (string, 50 chars, nullable): Name of external service called
- `ExternalServiceResponse` (string, JSON, nullable): Response from external service (sensitive data masked)
- `IPAddress` (string, 45 chars, nullable): User's IP address
- `UserAgent` (string, 500 chars, nullable): Browser/client information
- `ChangeReason` (string, 200 chars, nullable): Reason for the change

**Business Rules**:
- All CRUD operations must generate audit log entries
- External service calls (SupplierService, OrderService) must be logged
- Audit logs are append-only (never updated or deleted)
- Retention period: 5 years
- JSON format for storing old/new values and external service responses
- Sensitive data should be masked in audit logs
- External service failures and retries should be logged

**Relationships**:
- No direct foreign key relationships (uses EntityType + EntityId)
- Tracks external service dependencies for troubleshooting

### 6. DomainEvent
**Purpose**: Stores domain events for event-driven architecture, particularly for triggering PDF generation and other async processes.

**Attributes**:
- `Id` (long, Primary Key): Unique identifier
- `EventType` (string, 100 chars): Type of event (e.g., "PurchaseOrderCreated", "PurchaseOrderUpdated")
- `AggregateId` (string, 50 chars): ID of the aggregate that generated the event
- `AggregateType` (string, 50 chars): Type of aggregate (e.g., "PurchaseOrder")
- `EventData` (string, JSON): Serialized event data
- `EventVersion` (int): Version of the event for backward compatibility
- `OccurredAt` (DateTime): When the event occurred
- `ProcessedAt` (DateTime, nullable): When the event was processed
- `CorrelationId` (string, 100 chars): Correlation ID for tracking related operations
- `UserId` (string, 50 chars): User who triggered the event
- `IsProcessed` (bool): Whether the event has been processed
- `ProcessingAttempts` (int): Number of processing attempts (for retry logic)
- `LastProcessingError` (string, 1000 chars, nullable): Last processing error message

**Business Rules**:
- EventType and AggregateId are required
- Events are immutable once created
- Failed processing should increment ProcessingAttempts
- Events should be processed exactly once (idempotent)
- Correlation ID helps track related operations across services
- Events trigger async operations like PDF generation via PdfService

**Relationships**:
- No direct foreign key relationships (uses AggregateId to reference entities)
- Events are processed by background services or message handlers

## Enumerations

### OrderStatus
- `Pending` (0): Initial status when order is created
- `Approved` (1): Order has been approved by manager/procurement
- `Ordered` (2): Order has been sent to supplier
- `Delivered` (3): Order has been received
- `Cancelled` (4): Order has been cancelled

### OrderType
- `Internal` (0): Purchase for company operations
- `External` (1): Purchase for client projects

### AddressType
- `Shipping` (0): Delivery address
- `Billing` (1): Billing address

### AuditAction
- `Create` (0): Entity was created
- `Update` (1): Entity was modified
- `Delete` (2): Entity was deleted
- `Approve` (3): Order was approved
- `Cancel` (4): Order was cancelled
- `ExternalFetch` (5): Data fetched from external service
- `ExternalValidation` (6): External service validation performed
- `PDFGenerated` (7): PDF was generated via PdfService
- `EventPublished` (8): Domain event was published

### DocumentType
- `CustomerPO` (0): Customer purchase order document
- `InternalApproval` (1): Internal approval documentation
- `Invoice` (2): Supplier invoice or proforma invoice
- `Reference` (3): Reference documents (specs, drawings, etc.)
- `GeneratedPDF` (4): Automatically generated PDF via PdfService
- `Other` (5): Other supporting documents

## Database Indexes

### Performance Optimization Indexes

**PurchaseOrder Table**:
- `IX_PurchaseOrders_OrderNumber` (Unique): Fast lookup by order number
- `IX_PurchaseOrders_CustomerPO`: Fast lookup by customer PO number
- `IX_PurchaseOrders_SupplierID`: Fast lookup by supplier
- `IX_PurchaseOrders_OrderID` (Unique): Fast lookup by referenced order (prevents duplicates)
- `IX_PurchaseOrders_CreatedBy_Status`: User's orders filtered by status
- `IX_PurchaseOrders_CreatedAt`: Date range queries for reporting
- `IX_PurchaseOrders_Status_OrderType`: Status and type filtering

**OrderItem Table**:
- `IX_OrderItems_PurchaseOrderId`: Fast retrieval of order items
- `IX_OrderItems_ProductCode`: Product-based searches

**PurchaseOrderFile Table**:
- `IX_PurchaseOrderFiles_PurchaseOrderId`: Fast retrieval of documents for a PO
- `IX_PurchaseOrderFiles_ObjectName` (Unique): Ensure unique object paths in GCS
- `IX_PurchaseOrderFiles_UploadedBy`: Track documents by uploader
- `IX_PurchaseOrderFiles_DocumentType`: Filter documents by type

**AuditLog Table**:
- `IX_AuditLog_EntityType_EntityId`: Audit history for specific entities
- `IX_AuditLog_UserId_Timestamp`: User activity tracking
- `IX_AuditLog_Timestamp`: Time-based audit queries

**DomainEvent Table**:
- `IX_DomainEvents_IsProcessed_OccurredAt`: Unprocessed events by creation time
- `IX_DomainEvents_EventType_AggregateId`: Events for specific aggregates
- `IX_DomainEvents_CorrelationId`: Track related operations
- `IX_DomainEvents_ProcessingAttempts`: Failed event retry queries

## Validation Rules

### Purchase Order Validation
- OrderNumber: Auto-generated, unique, format "PO-YYYY-######"
- CustomerPO: Optional, 1-50 characters if provided, alphanumeric with dashes/underscores
- SupplierName: Required, 1-100 characters
- TotalAmount: Must equal sum of OrderItems.TotalPrice
- Status transitions must follow business rules
- Only authorized users can approve orders

### Order Item Validation
- ProductName: Required, 1-200 characters
- Quantity: Must be > 0, max 3 decimal places
- UnitPrice: Must be >= 0, max 2 decimal places
- Currency: Must match parent PurchaseOrder

### Address Validation
- ContactName: Required, 1-100 characters
- AddressLine1: Required, 1-100 characters
- City: Required, 1-50 characters
- PostalCode: Required, format validation by country
- Country: Required, 1-50 characters
- EmailAddress: Valid email format if provided

### PurchaseOrderFile Validation
- FileName: Required, 1-255 characters, valid filename format
- ObjectName: Required, 1-500 characters, unique GCS object path
- FileSize: Must be > 0 and <= 50MB
- ContentType: Must be valid MIME type (PDF, images, Office docs allowed)
- DocumentType: Must be valid DocumentType enum value
- Only authorized users with PO access can upload/download documents
- GeneratedPDF documents are created automatically and cannot be uploaded manually

### DomainEvent Validation
- EventType: Required, 1-100 characters, valid event type format
- AggregateId: Required, 1-50 characters
- AggregateType: Required, 1-50 characters
- EventData: Required, valid JSON format
- EventVersion: Must be >= 1
- CorrelationId: Required for tracking related operations
- Events are immutable once created
- ProcessingAttempts should not exceed 5 retries

## Concurrency Control

### Optimistic Concurrency Strategy
- **PurchaseOrder**: Uses `RowVersion` timestamp for conflict detection
- **OrderItem**: Inherits concurrency protection through parent PurchaseOrder
- **Address**: Uses `LastModifiedAt` for basic change tracking
- **PurchaseOrderFile**: Uses `UploadedAt` for basic change tracking, prevent duplicate uploads
- **AuditLog**: Append-only, no concurrency concerns
- **DomainEvent**: Append-only, no concurrency concerns, uses `IsProcessed` for idempotent processing

### Conflict Resolution
- Update conflicts return HTTP 409 with current entity state
- Client must refresh and reapply changes
- Automatic retry for transient conflicts (max 3 attempts)

## Data Retention and Archival

### Active Data
- **Purchase Orders**: Maintain indefinitely for business operations
- **Order Items**: Linked to parent purchase order lifecycle
- **Addresses**: Maintain while referenced by active orders

### Audit Data
- **Audit Logs**: Retain for 5 years from creation date
- **Archive Process**: Move logs older than 5 years to cold storage
- **Compliance**: Maintain audit trail integrity during archival

## Security Considerations

### Data Protection
- Sensitive financial data encrypted at rest
- Personal information (contact details) protected under privacy regulations
- Audit logs exclude sensitive authentication tokens

### Access Control
- Row-level security based on user roles and ownership
- Employees can only access their own orders
- Managers can access orders in their department
- Procurement team has full access
- Admin role for system maintenance and audit access

This data model supports all functional requirements while providing scalability, maintainability, and security for the PurchaseOrderService microservice.