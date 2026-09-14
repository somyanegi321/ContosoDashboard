# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 10 SDK installed and available on PATH.
- PowerShell in the repository root.
- A clean local SQLite database for the first run.
- A configured local malware scanner adapter that can return Clean, Infected, and Unavailable outcomes for tests.

To reset the training database before validating schema changes, stop the app and remove `ContosoDashboard/ContosoDashboard.db`; the next start recreates the schema with `EnsureCreated()`. The configured local adapter treats ordinary files as Clean and filenames containing `eicar` as Infected. Use a non-`Local` `Documents:ScannerMode` to exercise the Unavailable fail-closed path.

## Start the application

```powershell
cd ContosoDashboard
dotnet restore
dotnet run
```

Open the local URL printed by the application and sign in with one of the existing mock users.

## Scenario 1: Valid upload and metadata

1. Sign in as an employee.
2. Open My Documents and upload a PDF smaller than 25 MB.
3. Supply a title and category; add a description, tags, and project.
4. Confirm progress and success feedback.
5. Confirm the document list shows title, category, date, size, project, uploader, and type.
6. Confirm the stored file is outside `wwwroot` and uses a protected unique path.

Expected: the document is available to its owner and authorized project members, and upload activity is recorded.

## Scenario 2: Validation and fail-closed security

Test each independently:

- Upload a file larger than 25 MB.
- Upload an unsupported extension.
- Return Infected from the scanner.
- Return Unavailable from the scanner.
- Force metadata persistence to fail after file write.

Expected: each attempt produces a clear failure, no rejected file is accessible, and file cleanup prevents orphaned metadata or content.

## Scenario 3: Authorization and IDOR protection

1. Upload a project document as a project member.
2. Sign in as another user who is not a member.
3. Attempt to open the project document list, search for the document, preview it, download it, edit it, and delete it using direct URLs.
4. Repeat as a project manager and administrator.

Expected: the unauthorized user receives no document metadata or content; the project manager and administrator receive only the access permitted by the contract.

## Scenario 4: Search, sharing, and notifications

1. Create documents with different titles, descriptions, categories, tags, projects, and uploaders.
2. Verify sort and filter options.
3. Search by each supported field and measure the result time.
4. Share a document with one user and one team/department.
5. Sign in as each recipient and open Shared with Me.

Expected: results contain only accessible documents, search meets the 2-second target for the agreed dataset, recipients see the document, and in-app notifications are created.

## Scenario 5: Workflow integration and dashboard

1. Open a task and attach an existing document.
2. Upload a new document from the task.
3. Verify both documents use the task's project association.
4. Upload at least six documents as one user.
5. Open the dashboard.

Expected: task documents are visible in task context, task uploads inherit the project, Recent Documents shows the five newest uploads, and the summary shows the user's document count.

## Scenario 6: Audit and reporting

1. Perform successful and denied upload, download, share, replace, and delete actions.
2. Sign in as an administrator and open the document report.
3. Sign in as a non-administrator and attempt the same report.

Expected: the audit log includes actor, document, action, time, and outcome; administrator reports show file types, active uploaders, and access patterns; non-administrators are denied.

## Automated checks

```powershell
dotnet build
```

Run the focused document service, storage, authorization, and endpoint tests when the test project is added. At minimum, the verification suite must cover path traversal, unauthorized retrieval, fail-closed scanning, cleanup after persistence failure, and integer/text schema constraints.
