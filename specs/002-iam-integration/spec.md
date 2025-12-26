# Feature Specification: Permission-Based Authorization Migration

**Feature Branch**: `002-iam-integration`  
**Created**: 2025-12-22  
**Status**: Draft  
**Input**: User description: "use the content in purchaseorder-specify.md as specifications"

## Clarifications

### Session 2025-12-22
- Q: How should permissions and roles be registered with the IAM service? → A: Auto-register on service startup/deployment
- Q: What is the caching strategy for user permissions? → A: Short-lived local cache (Time-to-Live)
- Q: How is resource-level access (ownership) handled? → A: Service-level ownership checks (Functional permission + Resource ownership)
- Q: How should authorization decisions be logged? → A: Log all decisions (Grants & Denials)
- Q: How should missing permissions in IAM be handled? → A: Default to Deny (Fail Secure)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Full Access Management (Priority: P1)

As an administrator, I want to have complete control over all purchase order operations so that I can manage the entire system without restrictions.

**Why this priority**: Essential for system maintenance and emergency interventions. It's the baseline role that ensures all functionality is accessible.

**Independent Test**: Login as a user with the `purchase-order-admin` role and verify that all operations (create, read, update, delete, approve, cancel, receive, export, supplier management, and budget management) are permitted.

**Acceptance Scenarios**:

1. **Given** a user with `purchase-order-admin` role, **When** attempting any operation in the service, **Then** access is granted.
2. **Given** a new permission is added to the system, **When** an admin attempts to use it, **Then** access is granted (assuming role is updated or permissions are wildcarded).

---

### User Story 2 - Procurement Lifecycle Management (Priority: P1)

As a procurement officer, I want to create orders, select suppliers, and check budgets so that I can initiate and manage the procurement process.

**Why this priority**: This is the core business process. Without this, the service cannot fulfill its primary purpose of managing purchase orders.

**Independent Test**: Login as a user with `purchase-order-procurement` role and verify they can create orders and select suppliers, but cannot approve orders.

**Acceptance Scenarios**:

1. **Given** a user with `purchase-order-procurement` role, **When** creating a new purchase order, **Then** the order is created successfully.
2. **Given** a user with `purchase-order-procurement` role, **When** attempting to approve a purchase order, **Then** access is denied.

---

### User Story 3 - Order Approval Flow (Priority: P2)

As an approver, I want to review and approve or cancel orders so that I can ensure financial and operational compliance.

**Why this priority**: Critical for financial control and preventing unauthorized spending.

**Independent Test**: Login as a user with `purchase-order-approver` role and verify they can approve/cancel orders and check budgets, but cannot create or delete orders.

**Acceptance Scenarios**:

1. **Given** a pending purchase order and a user with `purchase-order-approver` role, **When** approving the order, **Then** the order status changes to Approved.
2. **Given** a user with `purchase-order-approver` role, **When** attempting to create a new order, **Then** access is denied.

---

### User Story 4 - Receiving and Verification (Priority: P2)

As a receiver, I want to mark goods as received so that I can record the physical arrival of items and trigger further processing.

**Why this priority**: Necessary for the final stage of the purchase order lifecycle before invoicing.

**Independent Test**: Login as a user with `purchase-order-receiver` role and verify they can mark orders as received, but have read-only access to everything else.

**Acceptance Scenarios**:

1. **Given** an approved purchase order and a user with `purchase-order-receiver` role, **When** marking items as received, **Then** the receiving record is updated.
2. **Given** a user with `purchase-order-receiver` role, **When** attempting to update order header information, **Then** access is denied.

---

### User Story 5 - Read-Only Auditing (Priority: P3)

As a viewer, I want to read purchase order details without making changes so that I can audit or monitor the procurement status.

**Why this priority**: Useful for reporting and transparency without risking data integrity.

**Independent Test**: Login as a user with `purchase-order-viewer` role and verify they can see all details but cannot perform any write/action operations.

**Acceptance Scenarios**:

1. **Given** a user with `purchase-order-viewer` role, **When** viewing any purchase order, **Then** the details are displayed.
2. **Given** a user with `purchase-order-viewer` role, **When** attempting any write operation (create, update, delete, approve, etc.), **Then** access is denied.

### Edge Cases

- **What happens when a user has multiple roles?** The system should grant the union of all permissions associated with those roles.
- **How does the system handle revoked permissions?** Access should be denied immediately or upon the next session/token validation if a permission is removed from a user's role.
- **What happens if the IAM service is unavailable?** The system should fail securely (deny access) and log the service failure.
- **What happens if a required permission is missing from IAM?** The system MUST default to Deny Access and log a critical configuration error (Fail Secure).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST register 13 specific permissions in the IAM service for Purchase Order, Supplier, and Budget operations.
- **FR-002**: System MUST define and register 6 predefined roles (`admin`, `manager`, `procurement`, `approver`, `receiver`, `viewer`) with their respective permission mappings.
- **FR-003**: System MUST migrate all existing policy-based authorization checks to use these fine-grained permissions.
- **FR-004**: Each API endpoint MUST be protected by the corresponding specific permission (e.g., `POST /orders` requires `purchase-order.orders.create`).
- **FR-005**: System MUST allow for hierarchical permission checking where applicable (e.g., `admin` having `purchase-order.*`).
- **FR-006**: System MUST automatically register permissions and roles with the IAM service during service startup or deployment.
- **FR-007**: System MUST perform resource-level ownership checks in service logic, complementing functional permission checks.
- **FR-008**: System MUST log all authorization decisions (grants and denials) including user ID, permission checked, and result.

### Non-Functional Requirements

- **NFR-001**: System MUST implement a 10-minute local cache for user permissions to minimize latency and IAM service load.

### Key Entities *(include if feature involves data)*

- **Permission**: A unique string identifier representing an atomic action (e.g., `purchase-order.orders.create`).
- **Role**: A collection of permissions assigned to users (e.g., `purchase-order-manager`).
- **Authorization Context**: The set of permissions currently held by the authenticated user, retrieved from the IAM service.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Exactly 13 functional permissions are successfully registered in the target environment.
- **SC-002**: 6 predefined roles are correctly configured with the mapped permissions as specified in the migration plan.
- **SC-003**: 100% of Purchase Order Service API endpoints enforce permission-based authorization.
- **SC-004**: Unauthorized access attempts result in a `403 Forbidden` response with no sensitive data leaked.
- **SC-005**: Authorized users can perform actions matching their permissions with an average authorization overhead of less than 5ms (using local cache).
