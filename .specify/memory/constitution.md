<!--
Sync Impact Report - Constitution Update
- Version change: 1.3.0 → 1.4.0
- Modified principles: Principle VI - Secrets Management & Configuration Security (STRENGTHENED)
- Added sections: Security Audit Requirements, DDOS Prevention Guidelines
- Removed sections: None
- Templates requiring updates:
  ✅ COMPLETED plan-template.md (Constitution Check section)
  ✅ COMPLETED spec-template.md (security requirements)
  ✅ COMPLETED tasks-template.md (security audit tasks)
- Follow-up TODOs:
  ✅ COMPLETED: Remediate exposed production API endpoints in appsettings.json
  ✅ COMPLETED: Remove database connection strings from source code
  ✅ COMPLETED: Sanitize JWT references in configuration files
  ✅ COMPLETED: Update documentation to use placeholder examples only
-->

# MALIEV Microservices Constitution

## Core Principles

### I. Service Autonomy (NON-NEGOTIABLE)

Each microservice must be **self-contained**:

* Own database and schema
* Own domain logic, independent of other services' internals
* Communicate with other services only via stable APIs or event streams
* No direct database access to another service

### II. Explicit Contracts

* All API endpoints must have **OpenAPI/Swagger documentation**
* Data contracts must be versioned (MAJOR.MINOR)
* Any schema or contract changes require backward-compatible migration or version bump

### III. Test-First Development (NON-NEGOTIABLE)

* Unit tests mandatory for all critical functionality
* Integration tests required for inter-service interactions
* Code must **fail tests before implementation**; adhere to Red-Green-Refactor cycle
* Minimum coverage: 80% for business-critical logic

### IV. Auditability & Observability

* All operations must log structured events (JSON, stdout) with traceable user/action info
* Audit logs must be tamper-proof and retained per company policy
* Services must expose lightweight health checks (liveness/readiness)

### V. Security & Compliance

* JWT-based authentication for service endpoints
* Role-based authorization enforced for all operations
* No sensitive data stored unencrypted
* Follow relevant regulations (e.g., GDPR, Thai taxation for withholding tax)

### VI. Secrets Management & Configuration Security (NON-NEGOTIABLE)

**CRITICAL SECURITY REQUIREMENTS:**

* **NEVER** store sensitive information in source code including:
  - Database connection strings (username, password, server details)
  - Production API endpoints (prevents DDOS attacks through exposed URLs)
  - JWT signing keys, secrets, or tokens
  - Service credentials, authentication tokens, or certificates
  - Production environment URLs or internal service addresses
  - Any configuration that could expose infrastructure topology

* **ALL** sensitive configuration MUST be provided via environment variables sourced from Google Secret Manager

* **SOURCE CODE RESTRICTIONS:**
  - Source code MUST contain ONLY placeholder examples marked as `<secret-value>` or `${ENVIRONMENT_VARIABLE_NAME}`
  - Localhost/development defaults are permitted ONLY for local development scenarios
  - Test configurations MAY contain non-production values but MUST NOT expose real infrastructure

* **DDOS PREVENTION:**
  - Public repositories MUST NOT contain production API endpoints
  - Documentation MUST use example domains (e.g., `https://example.com/api`) or placeholder patterns
  - Internal service URLs MUST be abstracted through environment configuration

* **MANDATORY SECURITY AUDIT:**
  - ALL commits MUST be scanned for exposed secrets before merge
  - Configuration files MUST be reviewed for production data leakage
  - Documentation MUST be sanitized of real infrastructure references
  - Violations MUST be remediated immediately with no exceptions

**Rationale**: Exposed endpoints enable DDOS attacks, credential leakage compromises security, and infrastructure exposure facilitates targeted attacks against production systems.

### VII. Zero Warnings Policy (NON-NEGOTIABLE)

* All builds MUST produce **zero warnings and zero errors**
* Exception: Preview versions of SDKs may generate warnings until stable release
* Code analysis rules MUST be enforced without suppressions
* Pull requests MUST NOT be merged with any build warnings
* Warnings MUST be treated as build failures in CI/CD pipeline

**Rationale**: Warnings indicate potential bugs, maintainability issues, or deprecated patterns that can lead to production failures and technical debt.

### VIII. Clean Project Artifacts (NON-NEGOTIABLE)

* ALL unused files MUST be deleted from the repository including:
  - Boilerplate files from project templates
  - Sample/example files not relevant to the project
  - Generated files that should be excluded (build artifacts, IDE files)
  - Outdated documentation or configuration files
  - Unused assets, images, or resources
* Only project-specific artifacts that serve the current implementation MUST remain
* Regular cleanup required during development and before each release
* Git ignore patterns MUST exclude all generated and temporary files

**Rationale**: Unused files create confusion, increase repository size, and can contain outdated or conflicting information that misleads developers.

### IX. Simplicity & Maintainability

* YAGNI principle: build only what is required
* Avoid global state; stateless services preferred
* Favor clear, readable code over clever optimizations
* Shared libraries allowed only if fully documented, tested, and versioned

## Deployment & Operations Standards

* All services must support containerization (Docker)
* Environment-specific configurations via environment variables
* Backups, scaling, monitoring, and recovery handled at infrastructure level
* Rate limiting and concurrency safeguards for critical endpoints

## Development Workflow

* Feature branches named `XXX-description` with automated CI/CD
* PRs must pass tests, code style, and review checks before merge
* Changes to contracts, critical logic, or infrastructure require explicit review and approval
* Versioning and changelogs mandatory for all service releases

## Security Compliance & Audit Requirements

### Pre-Commit Security Checklist

**MANDATORY** verification before ANY commit to public repositories:

* ✅ No production API endpoints in configuration files
* ✅ No database connection strings with real credentials
* ✅ No JWT signing keys or authentication secrets
* ✅ No production service URLs or internal addresses
* ✅ Documentation uses only placeholder/example domains
* ✅ Test configurations contain no production references

### Security Violation Response

* **IMMEDIATE REMEDIATION REQUIRED** for any exposed secrets or endpoints
* Compromised credentials MUST be rotated within 24 hours
* Production systems MUST be monitored for unauthorized access following exposure
* Security incidents MUST be reported to engineering leadership immediately

### Continuous Security Monitoring

* Automated scanning for secrets in all pull requests
* Regular audit of configuration files and documentation
* Quarterly review of security compliance across all services
* Security training mandatory for all developers with repository access

## Governance

* Constitution supersedes any individual developer preference
* All PRs must verify compliance with constitution principles AND security requirements
* Amendments require documentation, approval by engineering leadership, and a migration plan
* Violations must be flagged and corrected before merge or deployment
* Security violations result in immediate pull request rejection

**Version**: 1.4.0 | **Ratified**: 2025-09-18 | **Last Amended**: 2025-09-22