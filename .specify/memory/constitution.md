<!--
Sync Impact Report
- Version change: uninitialized scaffold -> 1.0.0
- Modified principles: five scaffold placeholders replaced with project principles
- Added sections: Project Constraints; Development Workflow
- Removed sections: none
- Follow-up TODOs: original ratification date is not recorded
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
ContosoDashboard MUST remain suitable for offline training and MUST NOT be presented as
production-ready. Features MUST preserve the documented mock authentication, local operation,
and educational simplicity unless a specification explicitly changes those constraints. This
keeps exercises reproducible and prevents training code from being mistaken for a deployable
production system.

### II. Layered Design and Replaceable Infrastructure
Business behavior MUST remain separated from persistence, authentication, storage, and other
infrastructure through services and explicit abstractions where a migration path is documented.
The default implementation MUST work offline with SQLite and local services. This preserves the
training architecture and keeps a future cloud migration from requiring business-logic rewrites.

### III. Authorization at Every Data Boundary
Protected pages MUST require authentication, and services MUST enforce authorization before
returning or mutating user- or project-scoped data. Implementations MUST prevent insecure direct
object references and MUST preserve user isolation. This defense-in-depth rule ensures that UI
visibility is never the only access control.

### IV. Verifiable Behavior
Every feature specification MUST define observable acceptance criteria. Changes MUST include
focused tests or an executable verification step for the affected behavior, and MUST pass restore,
build, and applicable tests before review. This makes instructional examples repeatable and
keeps regressions visible.

### V. Simple, Accessible User Workflows
User-facing changes MUST use the existing Blazor and Bootstrap conventions unless a specification
justifies a change. Workflows MUST provide clear states for loading, empty, success, and failure,
and MUST remain usable with keyboard navigation and readable labels. This keeps the dashboard
teachable, predictable, and usable across its core scenarios.

## Project Constraints

The application MUST target the .NET SDK version required by the project file and MUST preserve
package compatibility with that target. The default data store is SQLite, external services are
not required for local operation, and mock authentication is for training only. Secrets MUST NOT
be committed to source control. Security-sensitive changes MUST document their threat model and
verification steps.

## Development Workflow

Work MUST proceed from a written feature specification to an implementation plan and ordered
tasks. Reviews MUST check each applicable principle, acceptance criterion, authorization boundary,
and verification result. A change is not complete until the project restores and builds cleanly
apart from explicitly accepted warnings, and any known limitation is recorded in its documentation
or review notes.

## Governance

This constitution governs feature specifications, plans, tasks, implementation, and reviews for
ContosoDashboard. Amendments MUST state the affected principles, rationale, compatibility impact,
and required follow-up work. A constitution amendment MUST be reviewed before dependent feature
artifacts are approved.

Versioning follows semantic versioning: MAJOR indicates incompatible governance changes, MINOR
indicates a new or materially expanded principle or section, and PATCH indicates a clarification
or non-semantic correction. Every review MUST verify compliance with all applicable MUST rules and
MUST record any justified exception with an owner and expiry or removal condition.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): original adoption date is not recorded | **Last Amended**: 2026-09-14
