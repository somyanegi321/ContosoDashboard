# Data Model: Document Upload and Management

## Document

Represents one current uploaded file and its searchable metadata.

| Field | Type/shape | Rules |
|---|---|---|
| DocumentId | Integer | Primary key; consistent with existing entities |
| Title | Text | Required; non-empty after trimming |
| Description | Text, nullable | Optional |
| Category | Text | Required; one of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| Tags | Text, nullable | Optional custom tags; normalized for search |
| OriginalFileName | Text | Display-only source name; never used as a storage path |
| FilePath | Text | Required relative protected storage reference; GUID-based filename |
| FileSize | Integer/long | Required; greater than 0 and no more than 25 MB |
| FileType | Text | Required MIME type; supports at least 255 characters |
| UploadedAt | Date/time | Required |
| UploadedByUserId | Integer | Required owner relationship to User |
| ProjectId | Integer, nullable | Optional relationship to Project |
| TaskId | Integer, nullable | Optional relationship to TaskItem; task upload must use the task project |

**Indexes**: UploadedByUserId + UploadedAt, ProjectId + UploadedAt, Category, UploadedAt. Search fields require queryable indexes or an equivalent bounded search strategy.

## DocumentShare

Represents access granted by a document owner to a user or team.

| Field | Type/shape | Rules |
|---|---|---|
| DocumentShareId | Integer | Primary key |
| DocumentId | Integer | Required relationship to Document |
| SharedWithUserId | Integer, nullable | One of user or team targets is required |
| SharedWithDepartment | Text, nullable | Supports existing Department claim/team semantics |
| SharedByUserId | Integer | Required relationship to User |
| SharedAt | Date/time | Required |

A document share MUST be unique for the same document and target. Removing a share revokes access immediately.

## DocumentActivity

Audit record for document operations.

| Field | Type/shape | Rules |
|---|---|---|
| DocumentActivityId | Integer | Primary key |
| DocumentId | Integer, nullable | Nullable for rejected requests that identify no existing document |
| ActorUserId | Integer, nullable | Nullable only for unauthenticated or unresolvable attempts |
| Action | Text | Upload, Download, Preview, UpdateMetadata, Replace, Delete, Share, or Denied |
| Outcome | Text | Succeeded, Rejected, or Denied |
| OccurredAt | Date/time | Required |
| Details | Text, nullable | Safe diagnostic context; never include file contents or secrets |

## Relationships and Access Rules

- User 1-to-many Document ownership.
- Project 1-to-many Documents; project members can view/download project documents.
- TaskItem 1-to-many Documents; task access is constrained by the task project.
- Document 1-to-many DocumentShare and DocumentActivity.
- User 1-to-many DocumentShare targets and DocumentActivity actors.
- Document owner can edit, replace, delete, and share their document.
- Project Manager can manage documents for projects they manage.
- Team Lead management scope is limited to the team rule defined by the existing Department and role claims.
- Administrator can access all documents and reports.
- Every list, search, preview, download, and mutation must apply these rules before returning content or metadata.

## Lifecycle

1. Validate title, category, file size, extension/MIME type, project/task authorization, and scan result.
2. Generate the protected relative path before persistence.
3. Write the file to local storage.
4. Save document metadata and audit activity.
5. Notify project members or shares after the access state is committed.
6. On metadata failure, remove the newly written file and record the failure when possible.
7. On replacement, preserve the prior valid file until the new file and metadata are committed.
8. On deletion, confirm authorization, remove metadata and protected file, then record the activity without retaining ordinary access.
