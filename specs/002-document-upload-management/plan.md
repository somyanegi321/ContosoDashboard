# Implementation Plan: Document Upload and Management

**Branch**: `002-document-upload-management` | **Date**: 2026-09-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-document-upload-management/spec.md`

## Summary

Add secure offline document upload and management to the existing ContosoDashboard Blazor Server application. The implementation will add integer-keyed document metadata and audit entities to EF Core SQLite, a service layer that enforces owner/project/team/administrator authorization, local protected filesystem storage behind `IFileStorageService`, fail-closed malware scanning behind `IMalwareScanner`, and Blazor workflows for upload, browsing, search, sharing, task/project attachments, dashboard summaries, and administrator reporting.

## Technical Context

**Language/Version**: C# on .NET 10.0  
**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core SQLite 10.0, existing cookie/mock authentication and notification services  
**Storage**: SQLite metadata plus local filesystem files outside `wwwroot`; storage abstraction preserves future Azure Blob replacement  
**Testing**: New focused unit/integration test project or equivalent executable verification; `dotnet build` and quickstart scenarios are required because no test project currently exists  
**Target Platform**: Offline-capable Windows/local web application  
**Project Type**: Single web application  
**Performance Goals**: Upload up to 25 MB within 30 seconds, lists up to 500 accessible documents within 2 seconds, search within 2 seconds, PDF/image preview within 3 seconds  
**Constraints**: Files must be scanned before availability; fail closed when scanning is unavailable; files outside `wwwroot`; no user-supplied storage paths; integer document IDs; text categories; existing mock authentication; no cloud dependency  
**Scale/Scope**: One existing training application, five prioritized user journeys, document metadata/search/audit plus project/task/dashboard integrations; no collaborative editing or version history

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The current `.specify/memory/constitution.md` is an unresolved scaffold, so no project-specific MUST/SHOULD rules can be extracted. This is recorded as a governance warning rather than treated as a passed project gate.

- Training/offline scope: PASS by feature constraint and repository architecture.
- Layered design: PASS; service and storage abstractions are preserved.
- Authorization at data boundaries: PASS in design; every query, mutation, and retrieval endpoint delegates to authorization logic.
- Verifiable behavior: PASS in design; quickstart scenarios and focused security tests are required.
- Accessible workflows: PASS in design; existing Blazor/Bootstrap surfaces and explicit loading/error states are retained.
- Constitution artifact: WARNING; restore the ratified constitution before final implementation review.

## Research Summary

See [research.md](research.md). Key decisions are fail-closed scanning, protected relative storage paths, compensating cleanup for filesystem/database boundaries, service-level authorization, reuse of existing notifications, and a bounded task attachment relationship.

## Project Structure

### Documentation (this feature)

```text
specs/002-document-upload-management/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── document-management-contract.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
│   └── ApplicationDbContext.cs              # Document, share, activity DbSets/configuration
├── Models/
│   ├── Document.cs
│   ├── DocumentShare.cs
│   ├── DocumentActivity.cs
│   └── Notification.cs                      # Document notification types
├── Services/
│   ├── DocumentService.cs
│   ├── FileStorageService.cs                # IFileStorageService + local implementation
│   ├── MalwareScanner.cs                    # IMalwareScanner + local/test adapter
│   ├── NotificationService.cs               # Document notifications
│   └── DashboardService.cs                  # Recent documents and count
├── Pages/
│   ├── Documents.razor
│   ├── SharedDocuments.razor
│   ├── ProjectDetails.razor                 # Project document section
│   ├── Tasks.razor or TaskDetails.razor     # Task attachment workflow
│   └── Index.razor                          # Recent documents widget/count
├── Shared/
│   └── NavMenu.razor                        # Document navigation
├── Program.cs                               # DI, protected retrieval endpoint, storage config
└── wwwroot/css/site.css                     # Focused document workflow styling
```

**Structure Decision**: Extend the existing single `ContosoDashboard` web project. Keep metadata and authorization in `Data`, `Models`, and `Services`; keep user workflows in existing/new Blazor pages; keep files outside the web root and expose only an authorization-checked retrieval endpoint. Add tests in a separate test project when implementation begins because the repository currently has no test project.

## Phase 0: Research Complete

The research artifact resolves the technology, storage, authorization, transaction-compensation, notification, and task-integration decisions. No unresolved technical-context clarifications remain.

## Phase 1: Design Complete

- [data-model.md](data-model.md) defines entities, fields, validation, relationships, access rules, and lifecycle.
- [contracts/document-management-contract.md](contracts/document-management-contract.md) defines routes, service boundaries, storage, scanner, and notification contracts.
- [quickstart.md](quickstart.md) defines runnable validation scenarios and expected outcomes.

## Constitution Check After Design

- No constitution conflict was introduced by the design.
- The design keeps offline local storage, layered services, service-level authorization, and verifiable workflows.
- The unresolved constitution scaffold remains a governance warning and must be corrected before implementation approval.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Separate malware scanner abstraction | The requirement mandates scanning, but the repository has no scanner implementation and core operation must remain offline | Approving files without a scanner violates the security requirement; hard-coding a vendor prevents offline training and migration |
| Authorized retrieval endpoint | Files must remain outside `wwwroot` and inaccessible to unauthorized users | Static files cannot enforce per-document authorization |
| Compensating filesystem cleanup | File storage and SQLite cannot share one atomic transaction | Saving metadata first creates orphaned or invalid records; distributed transactions are disproportionate |
