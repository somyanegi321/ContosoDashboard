# ContosoDashboard Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

### I. Layered Offline Design
<!-- Example: I. Library-First -->
Core workflows remain usable offline and business behavior stays behind services and replaceable infrastructure abstractions.
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### II. Authorization At Data Boundaries
<!-- Example: II. CLI Interface -->
Every query, mutation, and file retrieval enforces authorization in the service or endpoint boundary, never only in the UI.
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### III. Verifiable Behavior
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
Feature work must have focused executable validation for security-sensitive and persistence behavior, with build validation before completion.
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### IV. Accessible Workflows
<!-- Example: IV. Integration Testing -->
User-facing workflows provide readable labels and explicit loading, empty, success, and failure states across responsive layouts.
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### V. Small, Traceable Changes
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
Prefer the simplest implementation that satisfies the requirement, preserve existing public behavior, and record safe diagnostic activity for important operations.
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## Additional Constraints
<!-- Example: Additional Constraints, Security Requirements, Performance Standards, etc. -->

Files remain outside `wwwroot`; document identifiers remain integer-backed; category values remain text; external services are optional for core training workflows.
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## Development Workflow
<!-- Example: Development Workflow, Review Process, Quality Gates, etc. -->

Changes follow the active feature task order, preserve unrelated user changes, and are validated at phase boundaries.
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

The constitution is reviewed with feature plans and implementation validation. Feature-specific constraints may add requirements but may not weaken these principles.
<!-- Example: All PRs/reviews must verify compliance; Complexity must be justified; Use [GUIDANCE_FILE] for runtime development guidance -->

**Version**: 1.0.0 | **Ratified**: 2026-09-14 | **Last Amended**: 2026-09-14
<!-- Example: Version: 2.1.1 | Ratified: 2025-06-13 | Last Amended: 2025-07-16 -->
