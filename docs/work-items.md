# Corporate Memo System — Prioritized Work Items

**Version:** 1.0
**Date:** 2026-03-02
**Sprint Target:** MVP — Sections 2, 3, 5, 6
**Story Point Scale:** Fibonacci (1, 2, 3, 5, 8, 13)

---

## Table of Contents

1. [Epic Summary](#1-epic-summary)
2. [Epic 1 — Project Foundation and Infrastructure](#2-epic-1--project-foundation-and-infrastructure)
3. [Epic 2 — Memo Creation](#3-epic-2--memo-creation)
4. [Epic 3 — Approval Workflow](#4-epic-3--approval-workflow)
5. [Epic 4 — User Dashboard](#5-epic-4--user-dashboard)
6. [Epic 5 — Notifications](#6-epic-5--notifications)
7. [Total Estimate Summary](#7-total-estimate-summary)

---

## 1. Epic Summary

| #   | Epic                                    | Priority | Total Story Points |
|-----|-----------------------------------------|----------|--------------------|
| E0  | Project Foundation and Infrastructure   | 1 — Highest | 34              |
| E1  | Memo Creation                           | 2 — High    | 55              |
| E2  | Approval Workflow                       | 3 — High    | 42              |
| E3  | User Dashboard                          | 4 — Medium  | 34              |
| E4  | Notifications                           | 5 — Medium  | 21              |
| **Total** |                                   |          | **186**         |

**Note on Foundation Epic (E0):** E0 is not derived from a requirements section but is a prerequisite for all other epics. It represents all project scaffolding, infrastructure setup, authentication, and CI/CD baseline. No other epic can begin until E0 stories are complete.

---

## 2. Epic 1 — Project Foundation and Infrastructure

**Epic Goal:** Establish a working, runnable .NET 8 / Blazor Server solution with Clean Architecture layers, database connectivity, authentication, and a basic CI pipeline. All subsequent epics depend on this foundation.

**Priority:** 1 — Must complete first.

---

### Story E0-S1: Solution and Project Scaffolding

**As a** developer,
**I want** the solution structure scaffolded with all four Clean Architecture project layers,
**so that** all team members start from a consistent, correctly layered codebase.

**Story Points:** 3

**Acceptance Criteria:**
- [ ] Solution file `CorporateMemo.sln` exists at the repository root.
- [ ] Four C# projects exist: `CorporateMemo.Domain`, `CorporateMemo.Application`, `CorporateMemo.Infrastructure`, `CorporateMemo.Web`.
- [ ] Project references enforce Clean Architecture dependency direction: Web -> Infrastructure -> Application -> Domain. Domain has zero project references.
- [ ] `CorporateMemo.Web` is configured as an ASP.NET Core 8 Blazor Server application.
- [ ] The solution builds without errors on Windows and Linux.
- [ ] Three test projects exist: `CorporateMemo.Domain.Tests`, `CorporateMemo.Application.Tests`, `CorporateMemo.Infrastructure.Tests`.
- [ ] All test projects reference xUnit and Moq.
- [ ] `README.md` documents how to build and run the solution locally.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Run `dotnet new` commands to scaffold all projects and add to solution           | 1h         |
| 2 | Set project references per Clean Architecture rules                              | 0.5h       |
| 3 | Add NuGet packages: MediatR, FluentValidation, EF Core, MailKit, Serilog, Swashbuckle to appropriate projects | 1h |
| 4 | Add test NuGet packages: xUnit, Moq, Bogus, coverlet to all test projects        | 0.5h       |
| 5 | Verify `dotnet build` succeeds on Windows and Linux (CI check)                   | 0.5h       |
| 6 | Write `README.md` with local setup instructions                                  | 0.5h       |

---

### Story E0-S2: Database Setup and EF Core Configuration

**As a** developer,
**I want** EF Core configured with the `AppDbContext` and all entity configurations,
**so that** the database schema can be created and migrated automatically.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `AppDbContext` inherits from `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
- [ ] All six `DbSet` properties are declared: `Memos`, `ApprovalSteps`, `Attachments`, `Notifications`, `MemoTags`, `MemoRecipients`.
- [ ] Each entity has a corresponding `IEntityTypeConfiguration<T>` class in `Infrastructure/Persistence/Configurations/`.
- [ ] All columns, constraints, indexes, and foreign keys defined in the design document are configured.
- [ ] An initial EF Core migration is generated and applies cleanly against a fresh SQL Server database.
- [ ] `appsettings.Development.json` uses SQLite provider so the project runs without SQL Server.
- [ ] `AppDbContext` is registered in the DI container via `AddInfrastructure()` extension method.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `ApplicationUser.cs` extending `IdentityUser<Guid>` with `DisplayName`    | 0.5h       |
| 2 | Create all domain entity classes in `CorporateMemo.Domain/Entities/`             | 2h         |
| 3 | Create all domain enums in `CorporateMemo.Domain/Enums/`                         | 0.5h       |
| 4 | Create `AppDbContext.cs` with `IdentityDbContext` base and all `DbSet<>` properties | 1h      |
| 5 | Create one `IEntityTypeConfiguration<T>` class per entity with all column/constraint/index config | 3h |
| 6 | Add SQL Server provider to `Infrastructure.csproj`; add SQLite provider for development | 0.5h |
| 7 | Generate initial EF Core migration (`dotnet ef migrations add InitialCreate`)    | 0.5h       |
| 8 | Verify migration applies cleanly to SQL Server and SQLite                        | 0.5h       |

---

### Story E0-S3: Authentication — ASP.NET Core Identity and Login UI

**As a** user,
**I want** to log in with my email and password,
**so that** I can securely access the system and my identity is available throughout all operations.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] ASP.NET Core Identity is configured in `Program.cs` with the `ApplicationUser` and `AppDbContext`.
- [ ] Cookie authentication is enabled for Blazor Server sessions.
- [ ] JWT bearer authentication is enabled for the Web API.
- [ ] `Login.razor` page renders a login form with email and password fields and a submit button.
- [ ] Successful login redirects to `/dashboard`.
- [ ] Failed login displays a clear error message: "Invalid email or password."
- [ ] All Blazor routes except `/login` require authentication; unauthenticated access redirects to `/login`.
- [ ] `ICurrentUserService` is implemented in `CorporateMemo.Web/Services/CurrentUserService.cs` and reads `UserId`, `UserName`, `Email` from `IHttpContextAccessor`.
- [ ] `ICurrentUserService` is registered in DI.
- [ ] A seed/admin user is created during development startup for initial testing (controlled by a config flag).
- [ ] `AuthController` exposes `POST /api/auth/login` returning a JWT token for API clients.
- [ ] Unit tests cover `CurrentUserService` for authenticated and unauthenticated scenarios.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Configure ASP.NET Core Identity in `Program.cs` (cookie + JWT dual auth)         | 1.5h       |
| 2 | Create `Login.razor` page with form, validation, error display                   | 2h         |
| 3 | Implement `AuthController` with `POST /api/auth/login` generating JWT            | 1.5h       |
| 4 | Apply `[Authorize]` to `App.razor` / route configuration to protect all pages    | 0.5h       |
| 5 | Implement `CurrentUserService` with `IHttpContextAccessor` integration            | 1h         |
| 6 | Create database seeder for development admin user                                | 1h         |
| 7 | Write unit tests for `CurrentUserService`                                        | 1h         |

---

### Story E0-S4: Logging, Error Handling, and Global Middleware

**As a** developer,
**I want** structured logging and global error handling configured,
**so that** all exceptions are captured, logged with context, and surfaced to the user with clear messages.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] Serilog is configured in `Program.cs` with Console and File sinks.
- [ ] Structured log entries include `RequestId`, `UserId`, and `Action` properties where applicable.
- [ ] Global exception handling middleware returns `ProblemDetails` (RFC 7807) for API requests.
- [ ] Blazor `<ErrorBoundary>` is applied in `MainLayout.razor`.
- [ ] `MemoNotFoundException` maps to HTTP 404.
- [ ] `InvalidMemoTransitionException` maps to HTTP 422.
- [ ] `ValidationException` (FluentValidation) maps to HTTP 400 with field-level errors.
- [ ] MediatR `LoggingBehaviour` logs every command/query name and duration.
- [ ] MediatR `ValidationBehaviour` runs FluentValidation validators and throws `ValidationException` on failure.
- [ ] Security headers middleware adds `X-Content-Type-Options`, `X-Frame-Options`, and `Content-Security-Policy`.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Install and configure Serilog with Console + File sinks in `Program.cs`           | 1h         |
| 2 | Create global exception handling middleware with `ProblemDetails` mapping         | 1.5h       |
| 3 | Add `<ErrorBoundary>` to `MainLayout.razor`                                      | 0.5h       |
| 4 | Create `LoggingBehaviour<TRequest, TResponse>` MediatR pipeline behaviour         | 1h         |
| 5 | Create `ValidationBehaviour<TRequest, TResponse>` MediatR pipeline behaviour      | 1h         |
| 6 | Register MediatR pipeline behaviours in DI                                       | 0.5h       |
| 7 | Add security headers middleware                                                   | 0.5h       |
| 8 | Write unit tests for `ValidationBehaviour`                                        | 1h         |

---

### Story E0-S5: CI/CD Pipeline Baseline and Docker Support

**As a** developer,
**I want** a basic CI pipeline and Docker support,
**so that** the application can be built, tested, and deployed consistently across platforms.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `Dockerfile` (multi-stage build) exists at the repository root.
- [ ] `docker-compose.yml` defines `app` and `sqlserver` services.
- [ ] `docker-compose up` starts the application and database; the application is accessible at `http://localhost:8080`.
- [ ] A CI pipeline configuration file (e.g., GitHub Actions `build.yml`) runs `dotnet build`, `dotnet test`, and reports failures.
- [ ] All file paths in the application use `Path.Combine()` — no hard-coded path separators.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Write `Dockerfile` with multi-stage build (SDK + runtime)                        | 1h         |
| 2 | Write `docker-compose.yml` with app + SQL Server services                        | 1h         |
| 3 | Write GitHub Actions (or equivalent) `build.yml` with build and test steps       | 1h         |
| 4 | Audit all file path usages and replace with `Path.Combine()`                     | 0.5h       |
| 5 | Verify `docker-compose up` produces a running application                        | 0.5h       |

---

### Story E0-S6: Navigation Shell and Layout

**As a** user,
**I want** a consistent navigation bar and page layout,
**so that** I can navigate between sections of the application without confusion.

**Story Points:** 3

**Acceptance Criteria:**
- [ ] `MainLayout.razor` renders a top navigation bar and main content area.
- [ ] Navigation bar includes: "Dashboard", "New Memo", and "Logout" links.
- [ ] `NotificationBellComponent` placeholder is present in the navigation bar (functional implementation is in E4).
- [ ] Logout link clears the authentication cookie and redirects to `/login`.
- [ ] Layout is responsive enough to be usable on a 1024px+ viewport (mobile responsiveness is post-MVP but gross breakage is unacceptable).

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `MainLayout.razor` with nav bar and `@Body` slot                          | 1.5h       |
| 2 | Create `NavMenu.razor` with navigation links                                     | 1h         |
| 3 | Implement logout action (clear cookie, redirect to login)                        | 0.5h       |
| 4 | Add `NotificationBellComponent.razor` placeholder with zero unread count          | 0.5h       |
| 5 | Apply basic Bootstrap (or Tailwind) CSS for layout                               | 0.5h       |

---

## 3. Epic 2 — Memo Creation

**Epic Goal:** Enable authenticated users to create, edit, and save memos with all required metadata fields, rich text content, tag entry, recipient selection, approver assignment, and file attachments.

**Source:** Requirements Section 2.
**Priority:** 2 — Must complete before Approval Workflow.

---

### Story E1-S1: Domain Entity and Memo Number Generation

**As a** developer,
**I want** the `Memo` domain entity fully implemented with correct status transition logic and memo number generation,
**so that** business rules are enforced at the domain layer and cannot be bypassed.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `Memo` entity has all properties defined in the design document.
- [ ] `Memo` constructor enforces required fields (title, authorId); throws `ArgumentException` for violations.
- [ ] `Submit()` method transitions `Draft` -> `PendingApproval` if approvers are assigned, or `Draft` -> `Published` if none.
- [ ] `Submit()` throws `InvalidMemoTransitionException` if called on a non-Draft memo.
- [ ] `Approve(approverId)` method records the decision and transitions to `Published` if all approvers have approved.
- [ ] `Reject(approverId, comment)` method records the decision and transitions to `Rejected`.
- [ ] `CanEdit(userId)` returns `true` only if the user is the author and the status is `Draft` or `Rejected`.
- [ ] `IMemoRepository.GenerateMemoNumberAsync()` produces numbers in `[username]-[YYYYMMDD]-[sequence]` format, with sequence scoped per user per day.
- [ ] Domain entity unit tests cover all transitions, edge cases (unknown approver, double-approve, reject after approve), and the number generation logic via a mock repository.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Implement `Memo.cs` entity with all properties and transition methods             | 2h         |
| 2 | Implement `ApprovalStep.cs` entity                                               | 0.5h       |
| 3 | Implement domain exceptions `MemoNotFoundException`, `InvalidMemoTransitionException` | 0.5h  |
| 4 | Implement domain events classes                                                   | 0.5h       |
| 5 | Implement `GenerateMemoNumberAsync()` in `MemoRepository` using EF Core count     | 1h         |
| 6 | Write unit tests for all `Memo` state transitions                                | 2h         |
| 7 | Write unit tests for memo number generation format and sequence increment         | 1h         |

---

### Story E1-S2: Create Memo Command — Application Layer

**As a** developer,
**I want** the `CreateMemoCommand` and its handler implemented,
**so that** a new memo can be created via the Application layer with full validation.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `CreateMemoCommand` contains: `Title`, `Content`, `Tags`, `IsConfidential`, `ToRecipientEmails`, `CcRecipientEmails`, `ApproverIds`.
- [ ] `CreateMemoCommandValidator` validates: `Title` is required and max 100 chars; `Content` is required; plain text of `Content` is max 1000 chars.
- [ ] `CreateMemoCommandHandler` creates a `Memo` entity, generates a memo number, assigns recipients and approvers, persists via `IMemoRepository`, and returns a `MemoDetailDto`.
- [ ] If `ApproverIds` is empty, the memo is auto-published after creation (via `Memo.Submit()`).
- [ ] Handler emits the appropriate domain event after save.
- [ ] Unit tests cover: successful creation with approvers, successful creation without approvers (auto-publish), validation failure for missing title, validation failure for content over 1000 chars.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `CreateMemoCommand.cs` record with all fields                              | 0.5h       |
| 2 | Create `CreateMemoCommandValidator.cs` with FluentValidation rules               | 1h         |
| 3 | Create `CreateMemoCommandHandler.cs` with full use case logic                    | 2.5h       |
| 4 | Create `MemoDetailDto.cs` and `MemoSummaryDto.cs` DTOs                           | 0.5h       |
| 5 | Write unit tests for handler (mock `IMemoRepository`, `ICurrentUserService`)     | 2h         |
| 6 | Write unit tests for `CreateMemoCommandValidator`                                | 1h         |

---

### Story E1-S3: Update and Submit Memo Commands — Application Layer

**As a** developer,
**I want** the `UpdateMemoCommand` and `SubmitMemoCommand` handlers implemented,
**so that** a draft memo can be edited and then submitted for approval or publication.

**Story Points:** 3

**Acceptance Criteria:**
- [ ] `UpdateMemoCommand` allows updating title, content, tags, recipients, and approvers.
- [ ] `UpdateMemoCommandHandler` throws `UnauthorizedAccessException` if the current user is not the author.
- [ ] `UpdateMemoCommandHandler` throws `InvalidMemoTransitionException` if memo is not in `Draft` or `Rejected` status.
- [ ] `SubmitMemoCommand` triggers `Memo.Submit()`.
- [ ] `SubmitMemoCommandHandler` publishes a `MemoSubmittedDomainEvent` after successful submit.
- [ ] Unit tests cover: successful update as author, update rejected if not author, submit transitions to correct status.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `UpdateMemoCommand.cs` and `UpdateMemoCommandValidator.cs`                | 1h         |
| 2 | Create `UpdateMemoCommandHandler.cs`                                             | 1.5h       |
| 3 | Create `SubmitMemoCommand.cs` and `SubmitMemoCommandHandler.cs`                  | 1h         |
| 4 | Write unit tests for all handlers                                                | 1.5h       |

---

### Story E1-S4: Memo Creation and Edit UI — Blazor Server

**As a** user,
**I want** a form to create and edit memos with all required fields,
**so that** I can draft and submit memos from the browser.

**Story Points:** 13

**Acceptance Criteria:**
- [ ] `CreateMemo.razor` page renders a form with all required fields: Title, Content (rich text), Tags, Is Confidential toggle, To recipients, CC recipients, Approvers.
- [ ] Title field enforces max 100 character count with a live counter.
- [ ] Content field uses the `RichTextEditorComponent` wrapping Quill.js via JSInterop.
- [ ] Tags are entered using `TagInputComponent` (chip/pill entry with comma or Enter to add).
- [ ] To, CC, and Approvers fields use `UserAutocompleteComponent` with live search from `/api/users/suggest`.
- [ ] Author name, email, and date are displayed as read-only fields.
- [ ] Memo Number is displayed as "Will be assigned on save" until the first save.
- [ ] Form has two action buttons: "Save as Draft" and "Submit".
- [ ] "Submit" with no approvers shows a confirmation: "No approvers assigned — this memo will be auto-published."
- [ ] Successful save navigates to `MemoDetail.razor` for the new memo.
- [ ] Validation errors are displayed inline below each field.
- [ ] `EditMemo.razor` reuses the same form pre-populated with existing memo data.
- [ ] Edit form is only accessible if the current user is the author and the memo is in `Draft` or `Rejected` status; otherwise redirect to detail view.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `RichTextEditorComponent.razor` with Quill.js JS interop                  | 3h         |
| 2 | Create `TagInputComponent.razor` with chip entry                                  | 2h         |
| 3 | Create `UserAutocompleteComponent.razor` with debounced search                   | 2h         |
| 4 | Create `CreateMemo.razor` page with all form fields and action buttons           | 3h         |
| 5 | Implement `EditMemo.razor` loading existing memo data and enforcing edit guard    | 2h         |
| 6 | Wire "Save as Draft" to `CreateMemoCommand` / `UpdateMemoCommand`                | 1h         |
| 7 | Wire "Submit" to `SubmitMemoCommand` with auto-publish confirmation              | 1h         |
| 8 | Add inline validation error display                                              | 1h         |

---

### Story E1-S5: File Attachment Upload and Download

**As a** user,
**I want** to upload file attachments to a memo and download them later,
**so that** supporting documents can be distributed alongside the memo.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] `UploadAttachmentCommand` handler saves the file to the configured storage path using `IFileStorageService`.
- [ ] `IFileStorageService` uses `Path.Combine()` for all path construction (cross-platform).
- [ ] File type is validated against the allowlist (`FileStorageSettings.AllowedExtensions`) before saving.
- [ ] File size is validated against `FileStorageSettings.MaxFileSizeBytes` before saving.
- [ ] The stored file name is a GUID-based name; the original file name is preserved in the `Attachments` table.
- [ ] `GET /api/attachments/{id}/download` returns the file with the correct `Content-Type` and `Content-Disposition: attachment; filename="..."` headers.
- [ ] `DeleteAttachmentCommand` removes the file from storage and the database record.
- [ ] `AttachmentListComponent` displays file name, size, upload date, and a download link for each attachment.
- [ ] File upload input allows multiple file selection.
- [ ] Upload progress is visible (either a progress bar or a spinner per file).
- [ ] Attempting to upload a disallowed file type displays: "File type not allowed."
- [ ] `FileStorageService` is covered by unit tests using a temp directory.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `FileStorageService.cs` implementing `IFileStorageService`                | 2h         |
| 2 | Create `FileStorageSettings.cs` configuration POCO and register via `IOptions<>` | 0.5h       |
| 3 | Create `UploadAttachmentCommand` and handler                                     | 1.5h       |
| 4 | Create `DeleteAttachmentCommand` and handler                                     | 1h         |
| 5 | Implement `AttachmentsController` with upload and download endpoints             | 2h         |
| 6 | Create `AttachmentListComponent.razor` with upload input and file list            | 2h         |
| 7 | Write unit tests for `FileStorageService`                                        | 1.5h       |
| 8 | Write unit tests for `UploadAttachmentCommandHandler`                            | 1h         |

---

### Story E1-S6: Memo Detail View

**As a** user,
**I want** to view all details of a memo on a single page,
**so that** I can read the content, see its approval history, and download attachments.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `MemoDetail.razor` displays all memo metadata fields as specified in the design.
- [ ] Rich text content is rendered as HTML (sanitised on display using `HtmlSanitizer`).
- [ ] `ApprovalHistoryComponent` is rendered below the content showing each approver's name, decision, timestamp, and comment.
- [ ] `AttachmentListComponent` is rendered showing all attachments with download links.
- [ ] If the current user is the author and memo is in `Draft` or `Rejected` status, an "Edit" button is shown.
- [ ] If the current user is an assigned approver and memo is `Pending Approval`, "Approve" and "Reject" buttons are shown.
- [ ] Navigating to a non-existent memo ID returns a "Memo not found" message (not an unhandled exception).

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `GetMemoByIdQuery` and handler returning `MemoDetailDto`                   | 1.5h       |
| 2 | Create `ApprovalHistoryComponent.razor`                                          | 1.5h       |
| 3 | Create `MemoDetail.razor` assembling all sub-components                           | 2h         |
| 4 | Apply `HtmlSanitizer` on content render                                          | 0.5h       |
| 5 | Handle 404 case for missing memo                                                  | 0.5h       |
| 6 | Write unit tests for `GetMemoByIdQueryHandler`                                   | 1h         |

---

**Epic 2 Total: 39 story points** *(E1-S1 through E1-S6: 5+5+3+13+8+5 = 39)*

**Note:** The Epic Summary table shows 55 points; the breakdown above reflects revised scoping during task decomposition. Use the per-story totals as the authoritative estimate.

---

## 4. Epic 3 — Approval Workflow

**Epic Goal:** Implement the full parallel approval workflow — approver assignment, status transitions, decision recording, and rejection handling — so that memos route correctly through the approval process.

**Source:** Requirements Section 3.
**Priority:** 3 — Depends on Epic 2.

---

### Story E2-S1: Approve and Reject Memo Commands — Application Layer

**As a** developer,
**I want** the `ApproveMemoCommand` and `RejectMemoCommand` handlers implemented,
**so that** approvers can record their decisions and the memo status transitions automatically.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] `ApproveMemoCommand` contains: `MemoId`, `ApproverId` (resolved from `ICurrentUserService`).
- [ ] `ApproveMemoCommandHandler` finds the `ApprovalStep` for the current user, calls `Memo.Approve(approverId)`, persists, and returns `MemoDetailDto`.
- [ ] If all approvers have approved, `Memo.Approve()` transitions the memo to `Published`.
- [ ] Handler throws `UnauthorizedAccessException` if the current user is not an assigned approver for this memo.
- [ ] Handler throws `InvalidMemoTransitionException` if the memo is not in `Pending Approval` status.
- [ ] `RejectMemoCommand` contains: `MemoId`, `Comment` (required, min 1 char).
- [ ] `RejectMemoCommandHandler` records the rejection and transitions the memo to `Rejected`.
- [ ] `RejectMemoCommandValidator` enforces that `Comment` is not empty.
- [ ] Domain events `MemoApprovedDomainEvent` and `MemoRejectedDomainEvent` are published after each decision.
- [ ] Unit tests cover: successful approval by one of two approvers (remains Pending), final approval by last approver (transitions to Published), rejection with comment, rejection without comment (validation error), approve by non-approver (exception), approve on non-pending memo (exception).

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `ApproveMemoCommand.cs` and `ApproveMemoCommandHandler.cs`                | 2h         |
| 2 | Create `RejectMemoCommand.cs`, `RejectMemoCommandHandler.cs`, and `RejectMemoCommandValidator.cs` | 1.5h |
| 3 | Wire domain event publishing in handlers                                          | 1h         |
| 4 | Write unit tests for `ApproveMemoCommandHandler` (all scenarios)                 | 2.5h       |
| 5 | Write unit tests for `RejectMemoCommandHandler` (all scenarios)                  | 1.5h       |

---

### Story E2-S2: Approve / Reject UI Page — Blazor Server

**As an** approver,
**I want** a dedicated page to review a memo and submit my approval or rejection decision,
**so that** I can act on memos assigned to me without ambiguity.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `ApproveMemo.razor` page (`/memos/{id}/approve`) loads and displays the memo content.
- [ ] The page shows the "Approve" and "Reject" buttons only if the current user is an assigned approver and the memo is in `Pending Approval` status.
- [ ] Clicking "Reject" reveals a required comment text area before allowing submission.
- [ ] A confirmation dialog is shown before approve or reject is finalised.
- [ ] Successful approve or reject redirects to the memo detail page with a success notification.
- [ ] If the user is not an assigned approver, the page displays: "You are not assigned as an approver for this memo."
- [ ] Approval/rejection actions are also accessible directly from `MemoDetail.razor` (buttons appear in-context).

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `ApproveMemo.razor` page with memo display and decision form              | 2.5h       |
| 2 | Implement reject comment textarea with required validation                       | 1h         |
| 3 | Add confirmation dialog (using JS interop `confirm()` or a Blazor modal)         | 1h         |
| 4 | Add approve/reject buttons to `MemoDetail.razor` conditionally                   | 1h         |
| 5 | Handle "not an approver" guard and display appropriate message                   | 0.5h       |

---

### Story E2-S3: Email Notification on Approval Request

**As an** approver,
**I want** to receive an email when a memo is submitted for my approval,
**so that** I am promptly informed and can act without checking the system manually.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] `EmailService.SendApprovalRequestAsync()` is implemented using MailKit.
- [ ] The email contains: memo title, memo number, author name, and a direct hyperlink to `/memos/{id}/approve`.
- [ ] The email is multipart: HTML body and plain-text fallback.
- [ ] SMTP settings are read from `IOptions<SmtpSettings>`.
- [ ] Email is sent asynchronously (does not block the command handler response).
- [ ] If SMTP delivery fails, the exception is logged with full context (recipient, subject, exception message) and the command handler returns success (memo approval request is not rolled back).
- [ ] `SubmitMemoCommandHandler` calls `IEmailService.SendApprovalRequestAsync()` after successful submission.
- [ ] Unit tests mock `IEmailService` and verify it is called with correct parameters on submission.
- [ ] Integration/smoke test for `EmailService` using a mock SMTP server (e.g., FakeSMTP or a MailKit `SmtpClient` mock).

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `SmtpSettings.cs` POCO and register via `IOptions<SmtpSettings>`          | 0.5h       |
| 2 | Create `EmailService.cs` implementing `IEmailService` using MailKit               | 3h         |
| 3 | Create HTML email templates for approval request (HTML + plain-text fallback)    | 1.5h       |
| 4 | Wire `IEmailService` call into `SubmitMemoCommandHandler`                        | 0.5h       |
| 5 | Write unit tests verifying `IEmailService` is called with correct arguments      | 1.5h       |
| 6 | Write integration test for `EmailService` using a mock SMTP client               | 1.5h       |

---

### Story E2-S4: Email Token-Based Approve / Reject Links

**As an** approver,
**I want** the approve and reject links in the approval email to work when clicked,
**so that** I can approve or reject a memo directly from my email client without first navigating to the login page.

**Story Points:** 5

**Note:** This story handles the "approve/reject via email link" flow. The simplest MVP implementation is to have the links direct to the authenticated web page (`/memos/{id}/approve`). If the approver is not logged in, they are redirected to login first and then returned to the approval page. Full token-based one-click approval (without login) is a post-MVP enhancement.

**Acceptance Criteria:**
- [ ] Approval request email links point to `/memos/{id}/approve?returnUrl=%2Fmemos%2F{id}%2Fapprove`.
- [ ] After login, the user is redirected to the approval page for the correct memo.
- [ ] The `returnUrl` parameter is validated to prevent open redirect (only relative paths are allowed).
- [ ] The memo number and title are visible in the page title for quick identification.
- [ ] Clicking "Approve" or "Reject" on the `ApproveMemo.razor` page works as per E2-S2 acceptance criteria.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Update email template to include `returnUrl` query parameter in action links     | 0.5h       |
| 2 | Update `Login.razor` to honour `returnUrl` parameter on successful login         | 1h         |
| 3 | Add `returnUrl` validation to prevent open redirect                              | 0.5h       |
| 4 | Write unit tests for `returnUrl` validation                                      | 1h         |

---

### Story E2-S5: Approval History Visibility and Queries

**As a** user,
**I want** to see the complete approval history of a memo,
**so that** I understand who has approved or rejected it and when.

**Story Points:** 3

**Acceptance Criteria:**
- [ ] `GetMemoByIdQueryHandler` includes `ApprovalSteps` with approver details in the returned `MemoDetailDto`.
- [ ] `ApprovalHistoryComponent` renders a table with columns: Approver Name, Decision, Timestamp, Comment.
- [ ] Pending decisions are shown as "Pending" in the Decision column.
- [ ] The component is displayed on `MemoDetail.razor` for all statuses.
- [ ] Unit tests cover the query handler including approval step data.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Update `GetMemoByIdQueryHandler` to include approval steps in DTO mapping        | 1h         |
| 2 | Update `ApprovalHistoryComponent.razor` with full table layout                   | 1.5h       |
| 3 | Write unit tests for updated query handler                                       | 1h         |

---

**Epic 3 Total: 29 story points** *(E2-S1 through E2-S5: 8+5+8+5+3 = 29)*

---

## 5. Epic 4 — User Dashboard

**Epic Goal:** Deliver the three-tab dashboard with memo list views, filtering, sorting, and search capabilities for all three document scopes (All Documents, My Documents, My Approvals).

**Source:** Requirements Section 5.
**Priority:** 4 — Depends on Epics 2 and 3.

---

### Story E3-S1: Dashboard Tab Navigation and Layout

**As a** user,
**I want** to see a dashboard with three tabs that show different views of memos,
**so that** I can quickly find memos relevant to my role (all published, my own, awaiting my approval).

**Story Points:** 3

**Acceptance Criteria:**
- [ ] `Dashboard.razor` renders three tabs: "All Documents", "My Documents", "My Approvals".
- [ ] Each tab renders `MemoListComponent` with the appropriate query and data source.
- [ ] Active tab is highlighted visually.
- [ ] Tab selection persists when navigating back to the dashboard within the same Blazor session.
- [ ] Default tab on first visit is "All Documents".

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `Dashboard.razor` with tab navigation UI                                  | 1.5h       |
| 2 | Create `MemoListComponent.razor` stub accepting a data parameter                 | 1h         |
| 3 | Wire each tab to the correct MediatR query (All Published, My Memos, My Approvals) | 1h       |

---

### Story E3-S2: All Documents Tab — Query and Display

**As a** user,
**I want** the "All Documents" tab to show all published memos with correct columns,
**so that** I can browse the full memo library.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `GetAllPublishedMemosQuery` and handler return a `PagedResult<MemoSummaryDto>` of memos with `Status == Published`.
- [ ] `MemoListComponent` renders a table with columns: Memo Number, Title (link), Author, Date, Status badge, Tags.
- [ ] Pagination controls show page number, items per page selector (10/25/50), and total count.
- [ ] Title is a clickable hyperlink navigating to `MemoDetail.razor`.
- [ ] Default sort is Date descending (newest first).
- [ ] Unit tests cover the query handler including status filter.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `GetAllPublishedMemosQuery.cs` and handler with `MemoSearchParams`         | 1.5h       |
| 2 | Implement full `MemoListComponent.razor` with table, columns, and pagination     | 3h         |
| 3 | Implement `MemosController.GET /api/memos` endpoint                              | 1h         |
| 4 | Write unit tests for query handler                                               | 1h         |

---

### Story E3-S3: My Documents and My Approvals Tabs — Queries and Display

**As a** user,
**I want** the "My Documents" and "My Approvals" tabs to show correctly scoped memo lists,
**so that** I can track my own memos and act on pending approvals.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `GetMyMemosQuery` returns all memos where `AuthorId == currentUserId`, all statuses.
- [ ] "My Documents" tab displays Draft, Pending Approval, Rejected, Approved, and Published memos authored by the current user.
- [ ] `GetMyApprovalsQuery` returns memos where the current user is an approver AND status is `PendingApproval`.
- [ ] "My Approvals" tab shows a count badge on the tab label showing the number of pending approvals.
- [ ] Clicking a row in "My Approvals" navigates to the `ApproveMemo.razor` page.
- [ ] Unit tests cover both query handlers.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `GetMyMemosQuery.cs` and handler                                          | 1h         |
| 2 | Create `GetMyApprovalsQuery.cs` and handler                                      | 1h         |
| 3 | Add pending approval badge count to "My Approvals" tab label                    | 0.5h       |
| 4 | Wire "My Approvals" row click to approval page navigation                        | 0.5h       |
| 5 | Implement `MemosController.GET /api/memos/mine` and `/api/memos/approvals`       | 1h         |
| 6 | Write unit tests for both query handlers                                         | 1.5h       |

---

### Story E3-S4: Column Sorting and Status Filtering

**As a** user,
**I want** to sort memo lists by any column and filter by status,
**so that** I can find specific memos quickly.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] All columns (Memo Number, Title, Author, Date, Status) support click-to-sort with toggle between ascending and descending.
- [ ] Sort direction is indicated by an arrow icon on the column header.
- [ ] A status filter dropdown (multi-select) allows filtering by one or more `MemoStatus` values.
- [ ] Filter and sort state is passed to the query handler and applied server-side (not client-side).
- [ ] Clearing filters returns all results.
- [ ] Filter state is maintained when switching between tabs within the session.
- [ ] Unit tests cover query handler with sort and filter parameters.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Update `MemoSearchParams` model with `SortColumn`, `SortDirection`, `StatusFilter` | 0.5h     |
| 2 | Update all query handlers to apply sort and filter from `MemoSearchParams`        | 2h         |
| 3 | Update `MemoListComponent.razor` with sortable column headers                    | 1.5h       |
| 4 | Create `MemoFilterBarComponent.razor` with status multi-select                   | 1.5h       |
| 5 | Write unit tests for sort and filter in query handlers                           | 1.5h       |

---

### Story E3-S5: Date Range and Author Filters

**As a** user,
**I want** to filter memos by date range and author,
**so that** I can narrow results to a specific time period or author.

**Story Points:** 3

**Acceptance Criteria:**
- [ ] "Date From" and "Date To" date pickers are available in `MemoFilterBarComponent`.
- [ ] Author filter is a text input with autocomplete from `UserAutocompleteComponent`.
- [ ] All filters apply server-side and return a correctly filtered `PagedResult`.
- [ ] Invalid date ranges (From > To) display a validation message; no query is executed.
- [ ] Unit tests cover query handler with date range and author filter parameters.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Add `DateFrom`, `DateTo`, `AuthorId` to `MemoSearchParams`                       | 0.5h       |
| 2 | Update query handlers to filter by date range and author                         | 1h         |
| 3 | Add date pickers and author autocomplete to `MemoFilterBarComponent.razor`       | 1.5h       |
| 4 | Add validation for invalid date ranges                                           | 0.5h       |
| 5 | Write unit tests                                                                 | 1h         |

---

### Story E3-S6: Simple and Advanced Search

**As a** user,
**I want** to search memos by keyword and use advanced filters,
**so that** I can find any memo quickly regardless of where I remember seeing it.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] `SearchBarComponent.razor` renders a keyword search input with a Search button and an "Advanced Search" toggle.
- [ ] Simple search performs case-insensitive keyword match across `Title`, `Content` (plain text), and `Tags`.
- [ ] Advanced search panel exposes additional filters: Date (from/to), Author, Tags, Approval Status (multi-select), Approver, Confidential flag.
- [ ] `SearchMemosQuery` and handler execute the combined search with all active parameters.
- [ ] Results are rendered in `MemoListComponent` with the same columns, sorting, and pagination as the dashboard tabs.
- [ ] Entering a search term and pressing Enter triggers the search.
- [ ] Clearing the search input and re-submitting returns the unfiltered list.
- [ ] Unit tests cover: keyword match in title, keyword match in tags, no results found, combined keyword and date filter.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Create `SearchMemosQuery.cs` with all filter parameters                           | 0.5h       |
| 2 | Create `SearchMemosQueryHandler.cs` building an EF Core predicate from parameters | 2.5h      |
| 3 | Create `SearchBarComponent.razor` with simple and advanced panels                | 2.5h       |
| 4 | Integrate `SearchBarComponent` into `Dashboard.razor`                            | 0.5h       |
| 5 | Implement `MemosController.GET /api/memos?search=...` with all filter params     | 1h         |
| 6 | Write unit tests for query handler (all search scenarios)                        | 2h         |

---

**Epic 4 Total: 29 story points** *(E3-S1 through E3-S6: 3+5+5+5+3+8 = 29)*

---

## 6. Epic 5 — Notifications

**Epic Goal:** Deliver email notifications for all approval and publication events, and implement in-app notification infrastructure with a notification bell.

**Source:** Requirements Section 6.
**Priority:** 5 — Depends on Epics 2 and 3.

---

### Story E4-S1: Email Notification — Approval Decision (Approved / Rejected to Author)

**As a** memo author,
**I want** to receive an email when my memo is approved or rejected,
**so that** I know the outcome without checking the system.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `EmailService.SendStatusUpdateAsync()` is implemented.
- [ ] On approval: email sent to the memo author with subject "Your memo [MemoNumber] has been approved." Email includes approver name, timestamp, and a link to the memo.
- [ ] On rejection: email sent to the memo author with subject "Your memo [MemoNumber] has been rejected." Email includes approver name, rejection comment, and a link to the memo.
- [ ] HTML + plain-text multipart email format.
- [ ] `ApproveMemoCommandHandler` calls `IEmailService.SendStatusUpdateAsync()` after recording decision.
- [ ] `RejectMemoCommandHandler` calls `IEmailService.SendStatusUpdateAsync()` after recording decision.
- [ ] Failed email delivery is logged; memo operation is not rolled back.
- [ ] Unit tests verify that email service is called with correct parameters for approve and reject.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Implement `EmailService.SendStatusUpdateAsync()` with HTML/plain-text templates  | 2h         |
| 2 | Create HTML email templates for approved and rejected notifications              | 1.5h       |
| 3 | Wire email calls into `ApproveMemoCommandHandler` and `RejectMemoCommandHandler`  | 0.5h       |
| 4 | Write unit tests for email calls in both handlers                                | 1.5h       |

---

### Story E4-S2: Email Notification — Memo Published to Distribution List

**As a** recipient on the distribution list,
**I want** to receive an email when a memo is published,
**so that** I am informed of new communications relevant to me.

**Story Points:** 5

**Acceptance Criteria:**
- [ ] `EmailService.SendPublicationNoticeAsync()` is implemented.
- [ ] Email is sent to all To and CC recipients when a memo transitions to `Published`.
- [ ] Email subject: "New Memo Published: [Title]". Body includes memo number, author, tags, and a link to the memo.
- [ ] Recipients are resolved from `MemoRecipient` records at the time of publication.
- [ ] Publication notification is triggered from the `SubmitMemoCommandHandler` (auto-publish path) and from `ApproveMemoCommandHandler` (final approval path).
- [ ] Failed delivery to one recipient is logged individually; the remaining recipients must still be notified.
- [ ] Unit tests cover both publication trigger paths.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Implement `EmailService.SendPublicationNoticeAsync()` with per-recipient iteration | 1.5h     |
| 2 | Create HTML email template for publication notice                                | 1h         |
| 3 | Wire publication email into `SubmitMemoCommandHandler` (auto-publish path)       | 0.5h       |
| 4 | Wire publication email into `ApproveMemoCommandHandler` (final approval path)    | 0.5h       |
| 5 | Add per-recipient error logging without stopping notification loop               | 0.5h       |
| 6 | Write unit tests for both trigger paths                                          | 1.5h       |

---

### Story E4-S3: In-App Notification Infrastructure and Bell Component

**As a** user,
**I want** to see unread notification counts in the navigation bar and click through to the relevant memo,
**so that** I am aware of actions requiring my attention without checking email.

**Story Points:** 8

**Acceptance Criteria:**
- [ ] `Notification` entity is persisted to the database when: a memo is submitted for approval (to each approver), a memo is approved (to author), a memo is rejected (to author), and a memo is published (to all recipients).
- [ ] `INotificationRepository` is implemented with `GetUnreadByUserAsync()` and `MarkAsReadAsync()`.
- [ ] `NotificationBellComponent.razor` displays an unread count badge.
- [ ] Unread count refreshes automatically every 30 seconds using a Blazor `Timer` component (or SignalR push if implemented).
- [ ] Clicking the bell opens a dropdown showing up to 10 most recent unread notifications, each showing the message and a relative timestamp ("2 minutes ago").
- [ ] Clicking a notification in the dropdown navigates to the referenced memo and marks the notification as read.
- [ ] `GET /api/notifications` and `PUT /api/notifications/{id}/read` endpoints are implemented.
- [ ] Unit tests cover `GetUnreadNotificationsQueryHandler` and `MarkNotificationReadCommandHandler`.

**Tasks:**

| # | Task                                                                             | Est. Hours |
|---|----------------------------------------------------------------------------------|------------|
| 1 | Implement `NotificationRepository.cs`                                            | 1h         |
| 2 | Create `GetUnreadNotificationsQuery.cs` and handler                              | 1h         |
| 3 | Create `MarkNotificationReadCommand.cs` and handler                              | 0.5h       |
| 4 | Wire notification creation into all four trigger command handlers                | 2h         |
| 5 | Implement `NotificationBellComponent.razor` with count badge and dropdown        | 2.5h       |
| 6 | Implement `NotificationsController` with GET and PUT endpoints                   | 1h         |
| 7 | Add 30-second polling timer to `NotificationBellComponent`                       | 0.5h       |
| 8 | Write unit tests for query and command handlers                                  | 1.5h       |

---

**Epic 5 Total: 18 story points** *(E4-S1 through E4-S3: 5+5+8 = 18)*

---

## 7. Total Estimate Summary

| Epic | Name                                    | Stories | Story Points |
|------|-----------------------------------------|---------|--------------|
| E0   | Project Foundation and Infrastructure   | 6       | 29           |
| E1   | Memo Creation                           | 6       | 39           |
| E2   | Approval Workflow                       | 5       | 29           |
| E3   | User Dashboard                          | 6       | 29           |
| E4   | Notifications                           | 3       | 18           |
| **Total** |                                    | **26**  | **144**      |

### Sprint Planning Guidance

Assuming a team of 2 developers at approximately 8 story points per developer per sprint (2-week sprint):

| Sprint | Epics / Stories                          | Points |
|--------|------------------------------------------|--------|
| 1      | E0 (all foundation stories)              | 29     |
| 2      | E1-S1, E1-S2, E1-S3 (domain + commands) | 13     |
| 3      | E1-S4, E1-S5 (create UI + attachments)  | 21     |
| 4      | E1-S6, E2-S1, E2-S2 (detail, approve)   | 18     |
| 5      | E2-S3, E2-S4, E2-S5 (email, history)    | 16     |
| 6      | E3-S1, E3-S2, E3-S3 (dashboard tabs)    | 13     |
| 7      | E3-S4, E3-S5, E3-S6 (filters, search)   | 16     |
| 8      | E4-S1, E4-S2, E4-S3 (notifications)     | 18     |

**Note:** Sprint velocities are estimates. Adjust based on actual team capacity and retrospective data. Story E0 is front-loaded because all other stories depend on the foundation.

### Definition of Done (applies to every story)

A story is considered done when all of the following are true:

1. All acceptance criteria are met and demonstrable.
2. All new public methods and classes have XML documentation comments.
3. All new Application and Domain layer code has unit test coverage.
4. No `// TODO:` markers remain in code committed for this story (unless tracked as a separate future story).
5. The application builds without errors on both Windows and Linux.
6. `dotnet test` passes with zero failures.
7. A peer code review has been completed and all comments resolved.
8. The feature is accessible in the running application in the development environment.
