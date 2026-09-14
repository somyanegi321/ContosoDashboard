# Research: Document Upload and Management

**Feature**: `002-document-upload-management`
**Date**: 2026-09-14

## Decision 1: Use the existing layered Blazor Server and EF Core architecture

- **Decision**: Add document entities and services to the existing single web project. Use Blazor pages for user workflows, EF Core SQLite for metadata, and service interfaces for authorization and storage.
- **Rationale**: The repository already uses integer-keyed EF entities, interface-based services, claim-based authentication, and SQLite. This satisfies the no-major-rewrite and offline training constraints.
- **Alternatives considered**: A separate document API or a second project was rejected because it would add deployment and authentication complexity without a requirement for independent scaling.

## Decision 2: Keep files outside `wwwroot` behind a storage abstraction

- **Decision**: Define `IFileStorageService` for upload, download, delete, and URL/reference operations. Implement local storage under an application data directory using relative, GUID-based paths. Serve content through an authorization-checked application endpoint.
- **Rationale**: Direct static-file access would bypass document permissions. A service boundary preserves the future Azure Blob migration path and keeps stored references portable.
- **Alternatives considered**: Storing files in `wwwroot` was rejected because it exposes files to unauthenticated static requests. Storing binary content in SQLite was rejected because it is less suitable for the stated local filesystem and cloud migration pattern.

## Decision 3: Fail closed when malware scanning is unavailable

- **Decision**: Define an `IMalwareScanner` boundary and require a clean result before a file becomes available. The local implementation must reject or quarantine files when scanning cannot produce a clean result; the scanner must not silently approve an unavailable scan.
- **Rationale**: The requirement explicitly requires scanning, while the repository has no scanner integration. A fail-closed boundary preserves security and allows a training-compatible scanner adapter to be supplied without coupling business logic to a vendor.
- **Alternatives considered**: Accepting files when no scanner is configured was rejected because it violates the security requirement. Adding a cloud-only scanner was rejected because core workflows must work offline.

## Decision 4: Make the upload sequence compensating rather than pretending it is transactional

- **Decision**: Validate metadata and permissions, generate the unique relative path, scan, write the file, then save metadata. If metadata persistence fails, delete the newly written file; if file persistence fails, do not save metadata. Replacements write a validated new file before switching metadata and remove the old file only after the new state is committed.
- **Rationale**: Filesystem writes and SQLite transactions cannot share one atomic transaction. Compensation prevents orphaned records and inaccessible or unexpectedly missing files.
- **Alternatives considered**: Saving a database row first was rejected because it can create empty or duplicate path records. A distributed transaction was rejected as disproportionate for the offline training app.

## Decision 5: Enforce access in the service and retrieval endpoint

- **Decision**: Every document query and mutation receives the requesting user context, applies owner/project/share/role authorization, and records allowed and denied activity. The download/preview endpoint calls the same authorization service before opening a stream.
- **Rationale**: Existing services already use explicit requesting-user authorization, and UI-only checks do not prevent IDOR or crafted requests.
- **Alternatives considered**: Relying on page visibility or route-level authorization alone was rejected because it does not protect direct file requests or service callers.

## Decision 6: Use the existing notification model and extend it with document event types

- **Decision**: Add document share and project-document notification types to the existing notification model/service. Notification failure must not grant access or roll back a completed document action; it must be recorded for diagnosis.
- **Rationale**: The application already has user-targeted in-app notifications and a notification service, so reuse keeps the feature consistent.
- **Alternatives considered**: Introducing email or an external notification provider was rejected because the feature must operate offline.

## Decision 7: Treat task integration as a bounded attachment relationship

- **Decision**: Add an optional task association to documents and expose related documents in the task workflow. Uploading from a task derives and validates the task project association. Because the current application lacks a task-detail page, add the smallest task detail surface needed for attachment workflows.
- **Rationale**: This meets the stakeholder requirement without introducing collaborative editing or a separate task subsystem.
- **Alternatives considered**: Adding a general-purpose document editor was rejected as out of scope. Attaching only by project was rejected because it loses task context.

## Resolved Risks and Open Implementation Notes

- The current application uses `EnsureCreated()`; implementation must update the database setup consistently and document the clean-state reset required for training.
- Team Lead management of team-member documents requires an explicit authorization rule because current project authorization primarily covers membership and project-manager roles.
- No automated test project exists; the plan therefore requires focused service tests or an executable integration verification project before review, with security scenarios prioritized.
