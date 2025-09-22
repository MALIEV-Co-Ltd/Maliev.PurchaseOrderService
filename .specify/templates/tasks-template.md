# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`
**Prerequisites**: plan.md (required), research.md, data-model.md, contracts/

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → If not found: ERROR "No implementation plan found"
   → Extract: tech stack, libraries, structure
2. Load optional design documents:
   → data-model.md: Extract entities → model tasks
   → contracts/: Each file → contract test task
   → research.md: Extract decisions → setup tasks
3. Generate tasks by category:
   → Setup: project init, dependencies, linting
   → Security: environment variable configuration, secrets validation
   → Quality: build validation, warning elimination, code analysis
   → Cleanup: unused file removal, template cleanup, artifact organization
   → Tests: contract tests, integration tests
   → Core: models, services, CLI commands
   → Integration: DB, middleware, logging
   → Polish: unit tests, performance, docs
4. Apply task rules:
   → Different files = mark [P] for parallel
   → Same file = sequential (no [P])
   → Tests before implementation (TDD)
5. Number tasks sequentially (T001, T002...)
6. Generate dependency graph
7. Create parallel execution examples
8. Validate task completeness:
   → All contracts have tests?
   → All entities have models?
   → All endpoints implemented?
9. Return: SUCCESS (tasks ready for execution)
```

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- Include exact file paths in descriptions

## Path Conventions
- **Single project**: `src/`, `tests/` at repository root
- **Web app**: `backend/src/`, `frontend/src/`
- **Mobile**: `api/src/`, `ios/src/` or `android/src/`
- Paths shown below assume single project - adjust based on plan.md structure

## Phase 3.1: Setup
- [ ] T001 Create project structure per implementation plan
- [ ] T002 Initialize [language] project with [framework] dependencies
- [ ] T003 [P] Configure linting and formatting tools

## Phase 3.2: Security Configuration ⚠️ BEFORE ANY IMPLEMENTATION
- [ ] T004 [P] Configure environment variables for external services (no hardcoded URLs)
- [ ] T005 [P] Implement Google Secret Manager integration for sensitive data
- [ ] T006 [P] Validate no secrets or connection strings in source code

## Phase 3.3: Quality Assurance ⚠️ ENFORCE ZERO WARNINGS
- [ ] T007 [P] Configure code analysis rules and linting (treat warnings as errors)
- [ ] T008 [P] Validate build produces zero warnings (except preview SDK)
- [ ] T009 [P] Set up CI/CD pipeline to fail on warnings

## Phase 3.4: Project Cleanup ⚠️ REMOVE UNUSED ARTIFACTS
- [ ] T010 [P] Remove boilerplate files and project template artifacts
- [ ] T011 [P] Delete unused example/sample files not relevant to project
- [ ] T012 [P] Clean up outdated documentation and configuration files
- [ ] T013 [P] Update .gitignore to exclude generated and temporary files

## Phase 3.5: Tests First (TDD) ⚠️ MUST COMPLETE BEFORE 3.6
**CRITICAL: These tests MUST be written and MUST FAIL before ANY implementation**
- [ ] T014 [P] Contract test POST /api/users in tests/contract/test_users_post.py
- [ ] T015 [P] Contract test GET /api/users/{id} in tests/contract/test_users_get.py
- [ ] T016 [P] Integration test user registration in tests/integration/test_registration.py
- [ ] T017 [P] Integration test auth flow in tests/integration/test_auth.py

## Phase 3.6: Core Implementation (ONLY after tests are failing)
- [ ] T018 [P] User model in src/models/user.py
- [ ] T019 [P] UserService CRUD in src/services/user_service.py
- [ ] T020 [P] CLI --create-user in src/cli/user_commands.py
- [ ] T021 POST /api/users endpoint
- [ ] T022 GET /api/users/{id} endpoint
- [ ] T023 Input validation
- [ ] T024 Error handling and logging

## Phase 3.7: Integration
- [ ] T025 Connect UserService to DB
- [ ] T026 Auth middleware
- [ ] T027 Request/response logging
- [ ] T028 CORS and security headers

## Phase 3.8: Polish
- [ ] T029 [P] Unit tests for validation in tests/unit/test_validation.py
- [ ] T030 Performance tests (<200ms)
- [ ] T031 [P] Update docs/api.md
- [ ] T032 Remove duplication
- [ ] T033 Run manual-testing.md

## Dependencies
- Security config (T004-T006) before quality (T007-T009)
- Quality assurance (T007-T009) before cleanup (T010-T013)
- Cleanup (T010-T013) before tests (T014-T017)
- Tests (T014-T017) before implementation (T018-T024)
- T018 blocks T019, T025
- T026 blocks T028
- Implementation before polish (T029-T033)

## Parallel Example
```
# Launch T014-T017 together:
Task: "Contract test POST /api/users in tests/contract/test_users_post.py"
Task: "Contract test GET /api/users/{id} in tests/contract/test_users_get.py"
Task: "Integration test registration in tests/integration/test_registration.py"
Task: "Integration test auth in tests/integration/test_auth.py"
```

## Notes
- [P] tasks = different files, no dependencies
- Verify tests fail before implementing
- Commit after each task
- Avoid: vague tasks, same file conflicts

## Task Generation Rules
*Applied during main() execution*

1. **From Contracts**:
   - Each contract file → contract test task [P]
   - Each endpoint → implementation task
   
2. **From Data Model**:
   - Each entity → model creation task [P]
   - Relationships → service layer tasks
   
3. **From User Stories**:
   - Each story → integration test [P]
   - Quickstart scenarios → validation tasks

4. **Ordering**:
   - Setup → Tests → Models → Services → Endpoints → Polish
   - Dependencies block parallel execution

## Validation Checklist
*GATE: Checked by main() before returning*

- [ ] All contracts have corresponding tests
- [ ] All entities have model tasks
- [ ] All tests come before implementation
- [ ] Parallel tasks truly independent
- [ ] Each task specifies exact file path
- [ ] No task modifies same file as another [P] task