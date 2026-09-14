# Tasks: Document Upload and Management

**Input**: Design documents from `specs/002-document-upload-management/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/document-management-contract.md](contracts/document-management-contract.md)
**Tests**: No TDD workflow was requested in the feature specification; each story includes an executable verification task, and final validation follows [quickstart.md](quickstart.md).

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish configuration and source locations for the offline document feature.

- [X] T001 Add document storage and upload limits configuration, including a protected `AppData/uploads` root and 25 MB maximum, in `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`
- [X] T002 [P] Add document feature constants for allowed categories, extensions, MIME types, and action names in `ContosoDashboard/Services/DocumentFeatureOptions.cs`
- [X] T003 [P] Add the document navigation entry and route labels in `ContosoDashboard/Shared/NavMenu.razor`
- [X] T004 [P] Create the feature verification notes and clean-database instructions in `specs/002-document-upload-management/quickstart.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared persistence, protected storage, scanning, and authorization boundaries before any user story work.

**CRITICAL**: Complete this phase before beginning user story implementation.

- [X] T005 Resolve the unresolved constitution scaffold and record the ratified project principles in `.specify/memory/constitution.md` before implementation approval
- [X] T006 Add integer-keyed `Document`, `DocumentShare`, and `DocumentActivity` entities with required fields, nullable project/task relationships, text category values, and MIME type capacity in `ContosoDashboard/Models/Document.cs`, `ContosoDashboard/Models/DocumentShare.cs`, and `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T007 Update `ContosoDashboard/Data/ApplicationDbContext.cs` with document DbSets, relationships, delete behavior, uniqueness constraints, and indexes from `specs/002-document-upload-management/data-model.md`
- [X] T008 Add the local protected file storage contract and implementation in `ContosoDashboard/Services/FileStorageService.cs`, including relative-path validation, GUID-based paths, directory creation, stream upload/download, idempotent delete, and rejection of absolute or traversal paths
- [X] T009 Add the malware scanning contract and fail-closed local/test adapter in `ContosoDashboard/Services/MalwareScanner.cs`, returning Clean, Infected, or Unavailable and never approving Infected or Unavailable files
- [X] T010 Add shared document authorization rules for owner, project member, project manager, team lead department scope, administrator, and current task access in `ContosoDashboard/Services/DocumentAuthorizationService.cs`
- [X] T011 Register document options, `IFileStorageService`, `IMalwareScanner`, `DocumentAuthorizationService`, and `IDocumentService` in `ContosoDashboard/Program.cs`; configure protected storage outside `wwwroot`
- [X] T012 Add the authorized download/preview endpoint in `ContosoDashboard/Program.cs` or `ContosoDashboard/Services/DocumentFileEndpoint.cs`, ensuring it authorizes before opening a stream and does not disclose inaccessible document metadata
- [X] T013 Update database initialization in `ContosoDashboard/Program.cs` and document the clean-state reset so the new schema is created consistently with the existing `EnsureCreated()` training workflow
- [X] T014 [P] Add the document notification event values and message construction hooks in `ContosoDashboard/Models/Notification.cs` and `ContosoDashboard/Services/NotificationService.cs`

**Checkpoint**: Shared entities, storage, scanning, authorization, routing, and DI are ready; no document page should bypass the service boundary.

---

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) 🎯 MVP

**Goal**: Authenticated employees can upload validated files with required metadata and see a secure document record.

**Independent Test**: Sign in as an employee, upload a supported file no larger than 25 MB with title and category, verify progress/success, and confirm the document metadata and protected file exist; repeat with oversized, unsupported, infected, and unavailable-scan inputs.

- [X] T015 [US1] Define upload request/result models and validation rules in `ContosoDashboard/Services/DocumentService.cs` for required title/category, optional description/project/tags, allowed types, 25 MB limit, and MIME type length
- [X] T016 [US1] Implement `IDocumentService.UploadAsync` in `ContosoDashboard/Services/DocumentService.cs` with project/task authorization, unique path generation before persistence, malware scan, file write, metadata save, audit activity, and compensating cleanup on failure
- [X] T017 [US1] Add the authenticated upload and My Documents page in `ContosoDashboard/Pages/Documents.razor` with multi-file selection, required metadata, category options, project selection, tags, progress, success/error states, and `@key` reset behavior
- [X] T018 [US1] Implement Blazor file buffering in `ContosoDashboard/Pages/Documents.razor` by capturing name/size/content type before opening the stream, copying to `MemoryStream`, clearing the browser file reference, and passing the buffered stream to the service
- [X] T019 [US1] Add upload-specific validation and user-facing error messages in `ContosoDashboard/Pages/Documents.razor` for size, type, scan, storage, and persistence failures without exposing server paths
- [X] T020 [US1] Add focused upload workflow styling for progress, validation, empty, success, and failure states in `ContosoDashboard/wwwroot/css/site.css`
- [X] T021 [US1] Verify the upload journey and rejected-file edge cases using the Scenario 1 and Scenario 2 steps in `specs/002-document-upload-management/quickstart.md`

**Checkpoint**: User Story 1 is independently demonstrable as the MVP.

---

## Phase 4: User Story 2 - Find, View, and Use Accessible Documents (Priority: P1)

**Goal**: Users can browse, filter, search, preview, and download only documents they are authorized to access.

**Independent Test**: Seed accessible and inaccessible documents, then verify My Documents, project documents, sort/filter/search, preview, download, and non-disclosure behavior against the contract.

- [X] T022 [US2] Implement accessible document query, sort, category/project/date filters, and search across title, description, tags, uploader, and project in `ContosoDashboard/Services/DocumentService.cs`
- [X] T023 [US2] Add paged list and search state handling to `ContosoDashboard/Pages/Documents.razor`, including title/category/date/size/project columns, loading state, empty state, and search timing instrumentation
- [X] T024 [P] [US2] Add the project document list and project-member download view in `ContosoDashboard/Pages/ProjectDetails.razor`, using the shared document authorization service
- [X] T025 [US2] Wire the authorized download and PDF/image preview actions from `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/ProjectDetails.razor` to the protected endpoint
- [X] T026 [US2] Add non-disclosing forbidden/not-found handling for inaccessible document IDs in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/DocumentFileEndpoint.cs`
- [X] T027 [US2] Verify list performance, filtering, search, project access, preview, download, and IDOR rejection using Scenario 3 and Scenario 4 in `specs/002-document-upload-management/quickstart.md`

**Checkpoint**: User Stories 1 and 2 are independently usable; unauthorized users cannot discover or retrieve document content or metadata.

---

## Phase 5: User Story 3 - Maintain and Share Documents (Priority: P2)

**Goal**: Owners and authorized project managers can update, replace, delete, and share documents with notification-backed access.

**Independent Test**: As an owner, edit metadata, replace the file, share with a user/team, confirm notification and Shared with Me visibility, then permanently delete; repeat management authorization as a project manager.

- [X] T028 [US3] Implement metadata update, replacement, permanent deletion, and share authorization in `ContosoDashboard/Services/DocumentService.cs`, preserving the prior valid file until replacement commit and recording every outcome
- [X] T029 [US3] Implement document share uniqueness, user/team target validation, immediate revocation, and share notification creation in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [X] T030 [US3] Add the Shared with Me page and share-management controls in `ContosoDashboard/Pages/SharedDocuments.razor` and `ContosoDashboard/Pages/Documents.razor`
- [X] T031 [US3] Add edit metadata, replace file, confirm delete, and share dialogs with loading/success/failure states in `ContosoDashboard/Pages/Documents.razor`
- [X] T032 [US3] Verify owner/project-manager permissions, replacement rollback, permanent deletion, share notification, recipient visibility, and revocation using Scenario 4 and Scenario 6 in `specs/002-document-upload-management/quickstart.md`

**Checkpoint**: User Story 3 is independently demonstrable without granting unauthorized management access.

---

## Phase 6: User Story 4 - Connect Documents to Workflows (Priority: P2)

**Goal**: Documents participate in task, project, dashboard, and project-notification workflows.

**Independent Test**: Attach an existing document and upload a new document from a task, verify task/project association, then verify dashboard recent documents and count after six uploads.

- [X] T033 [US4] Add task association and task-project consistency validation to document queries and upload/update workflows in `ContosoDashboard/Services/DocumentService.cs`
- [X] T034 [US4] Add the smallest task detail/attachment surface needed for related documents in `ContosoDashboard/Pages/TaskDetails.razor` or the existing task route in `ContosoDashboard/Pages/Tasks.razor`
- [X] T035 [US4] Add attach-existing and upload-from-task controls that automatically apply the task project in `ContosoDashboard/Pages/TaskDetails.razor` or `ContosoDashboard/Pages/Tasks.razor`
- [X] T036 [US4] Add recent-document and document-count queries to `ContosoDashboard/Services/DashboardService.cs` and expose them through the existing dashboard summary model
- [X] T037 [US4] Add the Recent Documents widget showing five latest user uploads and document count to `ContosoDashboard/Pages/Index.razor`
- [X] T038 [US4] Add project-document notification fan-out for eligible members after committed upload in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [X] T039 [US4] Verify task attachment/project inheritance, dashboard five-item limit/count, and project notifications using Scenario 5 in `specs/002-document-upload-management/quickstart.md`

**Checkpoint**: User Story 4 is independently demonstrable within the existing task, project, and dashboard workflows.

---

## Phase 7: User Story 5 - Audit Document Activity (Priority: P3)

**Goal**: Administrators can review complete document activity and usage reports while non-administrators remain denied.

**Independent Test**: Perform successful and denied upload, download, preview, share, replace, and delete actions, then compare administrator reports with recorded activity and attempt the report as a non-administrator.

- [X] T040 [US5] Implement activity recording for uploads, downloads, previews, metadata updates, replacements, deletes, shares, rejected files, and denied access in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Program.cs`
- [X] T041 [US5] Implement administrator-only aggregations for document types, active uploaders, and access patterns in `ContosoDashboard/Services/DocumentService.cs`
- [X] T042 [US5] Add the administrator document report page and authorization guard in `ContosoDashboard/Pages/DocumentReport.razor`
- [X] T043 [US5] Verify audit completeness, report values, safe diagnostic details, and non-administrator denial using Scenario 6 in `specs/002-document-upload-management/quickstart.md`

**Checkpoint**: All five user stories are independently demonstrable and security/audit behavior is visible.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validate the completed feature across performance, security, accessibility, documentation, and build quality.

- [X] T044 [P] Add focused document-service and storage verification coverage for path traversal, unauthorized retrieval, fail-closed scanning, cleanup after persistence failure, integer IDs, text categories, and replacement preservation in `ContosoDashboard.Verification/DocumentFeatureVerification.cs`
- [X] T045 [P] Review all document pages for keyboard navigation, readable labels, loading/empty/error states, and responsive layout in `ContosoDashboard/Pages/Documents.razor`, `ContosoDashboard/Pages/SharedDocuments.razor`, `ContosoDashboard/Pages/TaskDetails.razor`, and `ContosoDashboard/wwwroot/css/site.css`
- [ ] T046 [P] Add document storage, scan, upload, and authorization configuration documentation to `README.md`
- [ ] T047 Run `dotnet restore` and `dotnet build` for `ContosoDashboard/ContosoDashboard.csproj` and resolve feature-introduced errors in `ContosoDashboard/`
- [ ] T048 Run every scenario in `specs/002-document-upload-management/quickstart.md`, record measured upload/list/search/preview timings, and document any accepted limitations in `specs/002-document-upload-management/quickstart.md`
- [ ] T049 Review the feature against all requirements in `specs/002-document-upload-management/spec.md` and update `specs/002-document-upload-management/checklists/requirements.md` with implementation evidence

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No feature implementation dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup; blocks every user story.
- **User Story 1 (Phase 3)**: Depends on Foundational; MVP increment.
- **User Story 2 (Phase 4)**: Depends on Foundational and the upload metadata/query contract from US1.
- **User Story 3 (Phase 5)**: Depends on Foundational and document records/storage from US1; shares browse/query behavior from US2.
- **User Story 4 (Phase 6)**: Depends on Foundational and document service behavior from US1-US2; integrates with existing task/project/dashboard surfaces.
- **User Story 5 (Phase 7)**: Depends on Foundational and activity-producing operations from US1-US4.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Completion Order

1. US1 Upload and Organize (P1, MVP)
2. US2 Find, View, and Use Accessible Documents (P1)
3. US3 Maintain and Share Documents (P2)
4. US4 Connect Documents to Workflows (P2)
5. US5 Audit Document Activity (P3)

### Parallel Opportunities

- Setup: T002, T003, and T004 can run in parallel after T001 if they touch separate files.
- Foundational: T006, T008, T009, and T014 can begin in parallel; T007 depends on entity shapes, and T010-T013 depend on the resulting contracts/entities.
- US2: T024 can run in parallel with T023 once the service query contract is stable; T025 and T026 follow endpoint availability.
- US3: T029 and T030 can proceed in parallel after T028's service contract is established.
- US4: T034, T036, and T038 can proceed in parallel after the task/document relationship contract is stable.
- US5: T040 and T041 can proceed in parallel after the activity schema and service operations are available.
- Polish: T044, T045, and T046 can proceed in parallel; T047-T049 follow implementation completion.

## Parallel Example: User Story 1

```text
After T005-T014 establish the foundation:

Task T015: Define upload request/result validation in ContosoDashboard/Services/DocumentService.cs
Task T017: Build upload and My Documents UI in ContosoDashboard/Pages/Documents.razor
Task T020: Add upload state styling in ContosoDashboard/wwwroot/css/site.css

Then complete T016, T018, T019, and T021 in dependency order.
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundational phases, including storage, fail-closed scanning, schema, authorization, and protected retrieval boundaries.
2. Complete US1 upload, metadata, validation, cleanup, and My Documents work.
3. Run US1 independent verification and the upload/security quickstart scenarios.
4. Stop for review/demo before adding search, sharing, workflow integration, or reporting.

### Incremental Delivery

1. Foundation ready.
2. US1 provides secure upload and metadata visibility.
3. US2 adds retrieval and search.
4. US3 adds lifecycle management and sharing.
5. US4 connects documents to existing work surfaces.
6. US5 adds audit reporting.
7. Polish verifies the full feature and documents known limitations.

### Notes

- Every task uses the required checkbox, sequential ID, optional `[P]` marker, story label for story phases, and a concrete file path.
- No task creates collaborative editing, version history, external integrations, mobile support, quotas, or soft-delete recovery because those are out of scope.
- The unresolved constitution scaffold is a blocking governance task until ratified project principles are restored.
