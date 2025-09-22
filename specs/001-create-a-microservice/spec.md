# Feature Specification: PurchaseOrderService Microservice

**Feature Branch**: `001-create-a-microservice`
**Created**: 2025-09-18
**Status**: Draft
**Input**: User description: "Create a microservice called 'PurchaseOrderService'. This service manages purchase orders (internal and external) of our company. It must support full CRUD operations and have its own independent database for managing the purchase orders information."

## Execution Flow (main)

```
1. Parse user description from Input
   If empty: ERROR "No feature description provided"
2. Extract key concepts from description
   Identify: actors, actions, data, constraints
3. For each unclear aspect:
   Mark with [NEEDS CLARIFICATION: specific question]
4. Fill User Scenarios & Testing section
   If no clear user flow: ERROR "Cannot determine user scenarios"
5. Generate Functional Requirements
   Each requirement must be testable
   Mark ambiguous requirements
6. Identify Key Entities (if data involved)
7. Run Review Checklist
   If any [NEEDS CLARIFICATION]: WARN "Spec has uncertainties"
   If implementation details found: ERROR "Remove tech details"
8. Return: SUCCESS (spec ready for planning)
```

---

## Quick Guidelines

* Focus on WHAT users need and WHY
* Avoid HOW to implement (no tech stack, APIs, code structure)
* Written for business stakeholders, not developers

### Section Requirements

* **Mandatory sections**: Must be completed for every feature
* **Optional sections**: Include only when relevant to the feature
* When a section doesn't apply, remove it entirely

### For AI Generation

When creating this spec from a user prompt:

1. **Mark all ambiguities**: Use [NEEDS CLARIFICATION: specific question] for any assumption you'd need to make
2. **Don't guess**: If the prompt doesn't specify something, mark it
3. **Think like a tester**: Every vague requirement should fail the "testable and unambiguous" checklist
4. **Common underspecified areas**:

   * User types and permissions
   * Data retention/deletion policies
   * Performance targets and scale
   * Error handling behaviors
   * Integration requirements
   * Security/compliance needs

---

## User Scenarios & Testing *(mandatory)*

### Primary User Story
As a procurement team member at Maliev Co. Ltd., I need to create purchase orders based on customer orders/quotations so that I can efficiently manage supplier fulfillment, maintain traceability, and ensure document management without manually re-entering items.

### Acceptance Scenarios
1. **Given** I am an authorized user, **When** I create a new purchase order with a valid SupplierID, OrderID, and CurrencyID, **Then** the system:
   * Assigns a unique order number
   * Fetches order items from OrderService/QuotationService
   * Calculates totals and applies withholding tax if configured
   * **If internal PO**: Generates a PDF document via PdfService and uploads it to UploadService
   * **If external (customer-sent) PO**: Uploads the customer-provided document to UploadService
   * Stores the purchase order with pending status
2. **Given** an invalid SupplierID, OrderID, or CurrencyID, **When** I attempt to create a purchase order, **Then** the system rejects the creation with a 400 Bad Request.
3. **Given** an existing purchase order, **When** I update delivery details, currency, withholding tax, or status, **Then** the system saves the changes, **regenerates PDF for internal POs only**, uploads it, and maintains an audit trail. Items remain read-only and derived from OrderService/QuotationService.
4. **Given** multiple purchase orders exist, **When** I search by SupplierID, OrderID, CurrencyID, date range, status, order number, or PO type, **Then** the system returns matching purchase orders with relevant details.
5. **Given** a purchase order is no longer needed, **When** I cancel or delete it, **Then** the system updates the status appropriately or removes the order from active records, respecting business rules and audit requirements.
6. **Given** I need to review order history, **When** I view a specific purchase order, **Then** the system displays all order details including derived items, pricing, currency, addresses, withholding tax, uploaded documents (internal/external), generated PDF metadata (internal POs only), status changes, and audit trail.
7. **Given** a customer sends their own purchase order, **When** I store it in the system, **Then** the system captures the customer’s purchase order number, uploads their document, and links it to the related quotation/order for traceability.

### Edge Cases
* What happens when referenced Order/Quotation is cancelled or modified after PO creation? The system must detect changes from OrderService/QuotationService and notify the user; PO items remain based on snapshot at creation unless user triggers update.
* What happens when attempting to create a PO for an OrderID already linked to another purchase order? The system must prevent duplicates, returning a 409 Conflict unless overridden by an admin role.
* How does the system handle duplicate purchase order numbers or conflicting order references? Unique constraints enforced; system logs conflicts and prevents creation.
* What occurs when required information (SupplierID, OrderID, CurrencyID) is missing or invalid? The system returns a 400 Bad Request with clear validation messages.
* How does the system handle addresses coming from SupplierService vs CustomerService? Addresses are resolved based on PO type; conflicts prompt user confirmation before saving.
* How does the system behave when attempting to delete a purchase order that has associated financial transactions? Deletion is blocked; user must cancel PO instead, retaining audit trail.
* How should withholding tax updates be handled if the PO is modified? Withholding tax is recalculated automatically and stored in PO record; audit trail must capture previous and updated values.
* How should automatic PDF generation failures be handled for internal POs? Should retries or alerts be triggered? The system retries up to N times, logs failures, marks PO as `PDF Pending`, and triggers alert to procurement team.
* How does the system handle customer-sent POs uploaded as documents without generating PDF? System stores uploaded document metadata and links to PO; no PDF generation occurs for ExternalPO.
* How long are uploaded documents retained and what is the deletion/archive policy? Documents are retained for 5 years, then archived automatically; access controls apply throughout lifecycle.
* How are concurrency conflicts on simultaneous PO updates handled? Optimistic concurrency checks enforce update restrictions; conflicting updates return 409 Conflict with user-friendly message.

---

## Requirements *(mandatory)*

### Functional Requirements

* **FR-001**: System MUST allow authorized users to create new purchase orders linked to:
  * Supplier via SupplierID (referencing SupplierService)
  * Customer order/quotation via OrderID (referencing OrderService)
  * Currency via CurrencyID (referencing CurrencyService)
  * Addresses derived from SupplierService or CustomerService
  * Classification: InternalPO or ExternalPO (customer-sent)
    The purchase order items are automatically derived from the referenced order/quotation. Invalid SupplierID, OrderID, or CurrencyID must be rejected with a 400 Bad Request.
* **FR-002**: System MUST assign unique purchase order numbers automatically upon creation.
* **FR-003**: System MUST support updating existing purchase order details including delivery information, currency, withholding tax, and status; items remain read-only.
* **FR-004**: System MUST provide search and filtering capabilities for purchase orders by SupplierID, OrderID, CurrencyID, date range, status, order number, and PO type (internal/external).
* **FR-005**: System MUST allow users to view complete purchase order details including all associated items, pricing, currency, addresses, withholding tax, uploaded documents, PDF metadata (internal POs only), and status history.
* **FR-006**: System MUST support marking purchase orders with different statuses (pending, approved, ordered, delivered, cancelled, PDF Pending).
* **FR-007**: System MUST enable deletion or cancellation of purchase orders when appropriate.
* **FR-008**: System MUST distinguish between internal purchase orders (for company operations) and external purchase orders (customer-sent).
* **FR-009**: System MUST persist all purchase order data independently while still referencing SupplierService, OrderService, and CurrencyService.
* **FR-010**: System MUST maintain data integrity and consistency for all purchase order operations.
* **FR-011**: System MUST authenticate and authorize users before allowing access to purchase order operations.
  * JWT Bearer tokens with role claims:
    * 'employee': create/view own POs
    * 'manager': approve/cancel POs in their department
    * 'procurement': full access
    * 'admin': override, audit, manage users
* **FR-012**: System MUST provide audit trail functionality.
  * Log all create, update, cancel, and approval actions
  * Include user ID, role, timestamp, action type, previous and updated values
  * Retain audit logs for 5 years, then archive.
* **FR-013**: System MUST handle concurrent access to purchase orders with optimistic concurrency.
  * Conflicts return HTTP 409
  * Applies to update, approve, cancel, delete operations.
* **FR-014**: Data backup and recovery handled by infrastructure/cronjobs outside the service; service must ensure transactional consistency if backup triggered.
* **FR-015**: System MUST allow applying withholding tax to purchase orders according to Thailand tax regulations.
  * Calculate based on configured rate per supplier/service type
  * Deduct withholding tax from total payable
  * Store withholding tax details in PO record
  * Provide reporting and audit trail for withholding tax
  * Automatically recalculate if PO modified.
* **FR-016**: System MUST support storing purchase order documents (files) in UploadService.
  * Users can attach one or more documents per purchase order (customer PO, internal approval, invoices, reference docs)
  * Each document is uploaded to UploadService in the maliev GCS bucket
  * System allows specifying ObjectName (path inside bucket) for organized storage
  * Each PO record must store metadata for uploaded files: filename, object path, upload timestamp, uploaded by user, document type (internal PDF, customer PO, invoice, etc.)
  * Documents can be retrieved later for viewing or download
  * Access control applies: only authorized users with permission to the PO can upload or retrieve files
  * Retention: documents MUST follow the same lifecycle as the associated PO (retained for 5 years, then archived)
  * **Automatic PDF Generation**:
    * Applies **only to InternalPO**
    * When an internal PO is **created or updated**, the system MUST automatically request PdfService to generate a PDF version of the PO
    * Generated PDF is uploaded to UploadService using the defined ObjectName/path
    * PurchaseOrderService stores metadata of generated PDF in the PO record
    * Automatic regeneration occurs whenever an internal PO is updated
    * If PDF generation fails, the PO MUST be marked as `PDF Pending` and the system MUST retry up to N times and log the failure
    * Front-end applications should **not** trigger PDF generation; they only retrieve the generated PDF
* **FR-017**: System MUST support capturing and storing **Customer Purchase Order Number** for ExternalPOs.
  * CustomerPONumber is required when classifying as ExternalPO
  * Stored alongside uploaded customer PO document for full traceability
* **FR-018**: System MUST prevent duplicate purchase orders for the same OrderID unless explicitly overridden by an admin role.

---

### Key Entities

* **Purchase Order**: Contains order number, SupplierID, OrderID, CurrencyID, order date, delivery date, status, total amount, withholding tax, classification (internal/external), CustomerPONumber (optional, external only). Items are read-only and derived from OrderService/QuotationService. Addresses come from SupplierService or CustomerService.
* **Order Item**: Derived from referenced Order/Quotation; includes product name, quantity, unit price, total price; read-only.
* **Supplier**: Managed by SupplierService; PO stores only SupplierID reference.
* **Currency**: Managed by CurrencyService; PO stores only CurrencyID reference.
* **Address**: Delivery and billing addresses for the purchase order; linked by PurchaseOrder.
* **Withholding Tax**: Includes amount, rate, tax type, and reference; stored per PO for audit and reporting.
* **Purchase Order Document**: Stores reference to files uploaded via UploadService. Fields: DocumentID, PurchaseOrderID, Filename, ObjectName, UploadedBy, UploadedAt, DocumentType (internal PDF, customer PO, invoice, reference docs).

---

## Review & Acceptance Checklist

*GATE: Automated checks run during main() execution*

### Content Quality

* [ ] No implementation details (languages, frameworks, APIs)
* [ ] Focused on user value and business needs
* [ ] Written for non-technical stakeholders
* [ ] All mandatory sections completed

### Requirement Completeness

* [ ] No [NEEDS CLARIFICATION] markers remain
* [ ] Requirements are testable and unambiguous
* [ ] Success criteria are measurable
* [ ] Scope is clearly bounded
* [ ] Dependencies and assumptions identified

---

## Execution Status

*Updated by main() during processing*

* [x] User description parsed
* [x] Key concepts extracted
* [x] Ambiguities marked
* [x] User scenarios defined
* [x] Requirements generated
* [x] Entities identified
* [ ] Review checklist passed

---