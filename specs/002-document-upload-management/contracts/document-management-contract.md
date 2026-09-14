# Document Management Contract

## User-Facing Routes and Workflows

| Surface | Required behavior | Authorization |
|---|---|---|
| `/documents` | My Documents list, upload entry point, search, sort, and filters | Authenticated user; results are owner, project, or shared documents the user may access |
| `/documents/shared` | Shared with Me list | Authenticated user; only active shares |
| `/projects/{projectId}` | Project documents section and project upload entry point | Project member for view/download; project manager for management |
| `/tasks/{taskId}` | Related documents, attach existing document, and upload from task | User may view the task; task project authorization applies |
| `/documents/{documentId}/download` | Authorized file download | Document access required |
| `/documents/{documentId}/preview` | Authorized inline preview for PDF and images | Document access required; unsupported preview falls back to download |
| `/admin/documents/report` | Usage and activity reports | Administrator only |

All routes must return a non-disclosing forbidden/not-found response for inaccessible documents. They must not reveal title, owner, project, path, or existence through errors.

## Service Contracts

### `IDocumentService`

The service owns document metadata, authorization, lifecycle orchestration, search, and audit behavior.

- `UploadAsync(requestingUserId, metadata, content)` -> accepted document result or per-file validation failure.
- `GetMyDocumentsAsync(requestingUserId, query)` -> paged/filterable accessible document summaries.
- `SearchAsync(requestingUserId, query)` -> accessible results within the performance target.
- `GetProjectDocumentsAsync(requestingUserId, projectId)` -> project-accessible summaries.
- `GetTaskDocumentsAsync(requestingUserId, taskId)` -> task-accessible summaries.
- `UpdateMetadataAsync(requestingUserId, documentId, changes)` -> updated result or denial.
- `ReplaceFileAsync(requestingUserId, documentId, content)` -> updated result or validation failure.
- `DeleteAsync(requestingUserId, documentId)` -> success/denial; permanent deletion.
- `ShareAsync(requestingUserId, documentId, targets)` -> shares and notification outcomes.
- `GetActivityReportAsync(requestingUserId, filters)` -> administrator-only report.

Every operation that accepts a document ID must authorize in the service, regardless of caller surface.

### `IFileStorageService`

- `UploadAsync(stream, relativePath, contentType)` -> stored relative reference.
- `DownloadAsync(relativePath)` -> readable stream only after caller authorization.
- `DeleteAsync(relativePath)` -> idempotent cleanup result.
- `GetUrlAsync(relativePath, expiration)` -> optional protected URL/reference; direct public URLs are not permitted for local storage.

The local implementation must reject absolute paths, path traversal, and user-supplied path components.

### `IMalwareScanner`

- `ScanAsync(stream, fileName, contentType)` -> Clean, Infected, or Unavailable.

Only Clean permits availability. Infected and Unavailable produce a user-facing rejection without storing an accessible file.

## Notification Contract

- Sharing a document creates an in-app notification for each recipient.
- Adding a document to a project creates an in-app notification for eligible project members.
- Notification messages must identify the document title and actor without exposing a protected storage path.
