# Corporate Memo System — Requirements Specification

**Version:** 1.0
**Date:** 2026-03-02
**Status:** Approved for MVP Development
**Source Document:** Corporate Memo System Requirements V1.0

---

## Table of Contents

1. [Overview](#1-overview)
2. [MVP Scope Definition](#2-mvp-scope-definition)
3. [Functional Requirements — MVP](#3-functional-requirements--mvp)
   - 3.1 [Memo Creation (Section 2)](#31-memo-creation-section-2)
   - 3.2 [Approval Workflow (Section 3)](#32-approval-workflow-section-3)
   - 3.3 [User Dashboard (Section 5)](#33-user-dashboard-section-5)
   - 3.4 [Notifications (Section 6)](#34-notifications-section-6)
4. [Functional Requirements — Out of Scope for MVP](#4-functional-requirements--out-of-scope-for-mvp)
   - 4.1 [Confidential Memo Handling (Section 4)](#41-confidential-memo-handling-section-4)
   - 4.2 [Additional Features (Section 7)](#42-additional-features-section-7)
   - 4.3 [Future Considerations (Section 8)](#43-future-considerations-section-8)
5. [Non-Functional Requirements](#5-non-functional-requirements)
   - 5.1 [Performance](#51-performance)
   - 5.2 [Security](#52-security)
   - 5.3 [Scalability](#53-scalability)
   - 5.4 [Platform and Deployment](#54-platform-and-deployment)
   - 5.5 [Reliability and Availability](#55-reliability-and-availability)
   - 5.6 [Maintainability and Code Quality](#56-maintainability-and-code-quality)
6. [Constraints and Assumptions](#6-constraints-and-assumptions)
7. [Acceptance Criteria Summary](#7-acceptance-criteria-summary)

---

## 1. Overview

The Corporate Memo System is an internal communication platform that enables employees to create, manage, and distribute formal memos with configurable approval workflows and controlled visibility. The system supports structured authoring, multi-step approval routing, notification delivery, and a personalised user dashboard.

The system is built on **.NET 8 / C# / ASP.NET Core** for the backend and **Blazor Server** for the frontend. The architecture follows Clean Architecture principles as mandated by the Project Constitution.

---

## 2. MVP Scope Definition

The MVP (Minimum Viable Product) is a Proof-of-Concept delivery targeting the four core functional areas listed below.

| Section | Feature Area         | MVP Status       |
|---------|----------------------|------------------|
| 2       | Memo Creation        | **In Scope**     |
| 3       | Approval Workflow    | **In Scope**     |
| 4       | Confidential Memos   | Out of Scope     |
| 5       | User Dashboard       | **In Scope**     |
| 6       | Notifications        | **In Scope**     |
| 7       | Additional Features  | Out of Scope     |
| 8       | Future Considerations| Out of Scope     |

---

## 3. Functional Requirements — MVP

### 3.1 Memo Creation (Section 2)

#### 3.1.1 Memo Metadata Fields

| Field              | Requirement                                                                                     |
|--------------------|-------------------------------------------------------------------------------------------------|
| Memo Number        | Auto-generated at initial save using the format `[username]-[YYYYMMDD]-[sequence]`. The sequence is a zero-padded counter scoped to the user and date (e.g., `jsmith-20260302-001`). |
| Title              | Free-text input. Supports all printable characters and special characters. Maximum 100 characters. Required field. |
| Author Name        | Read-only. Auto-populated from the authenticated user's display name at the time of creation. |
| Author Email       | Read-only. Auto-populated from the authenticated user's registered email address.             |
| Date Created       | System-generated UTC timestamp recorded at the moment of the initial save action.             |
| Approved Date      | System-generated UTC timestamp recorded at the moment the final approval is granted. Null until approved. |
| Content            | Rich text editor (supports bold, italic, bullet lists, hyperlinks). Maximum 1000 characters of plain-text equivalent. |
| Hash Tags          | User-defined comma-separated or chip-entry keywords for search and categorisation. No maximum defined; reasonable UI limit of 20 tags. |
| Distribution List — To | Multi-select field with auto-suggestion from the internal user database (email and display name). One or more recipients required when submitting for approval or publishing. |
| Distribution List — CC | Multi-select field with auto-suggestion from the internal user database. Optional. |
| Approval Status    | System-managed enum. Valid values: `Draft`, `Pending Approval`, `Approved`, `Rejected`, `Published`. Default on creation: `Draft`. |
| Confidential Flag  | Boolean toggle. Stored in the database. UI rendering of confidentiality rules is deferred to post-MVP; the flag must still be persisted and visible in the MVP UI. |

#### 3.1.2 Attachments

- A memo may have zero or more file attachments.
- Supported file types: PDF, DOCX, XLSX, PNG, JPG, JPEG, GIF, BMP. Additional types may be added via configuration.
- Maximum file size per attachment: configurable via application settings (default: 10 MB).
- Total attachment size per memo: configurable (default: 50 MB).
- Multiple files may be uploaded simultaneously or sequentially.
- Uploaded files must be stored securely and referenced by the memo record.
- File preview: optional in MVP; at minimum, file name and size must be displayed with a download link.
- File names must be sanitised before storage to prevent path traversal vulnerabilities.

#### 3.1.3 Memo Lifecycle Actions

| Action           | Precondition                                        | Resulting Status     |
|------------------|-----------------------------------------------------|----------------------|
| Save as Draft    | Any state where the memo has not been published     | `Draft`              |
| Submit           | Approver(s) assigned                                | `Pending Approval`   |
| Auto-Publish     | Submit with no approvers assigned                   | `Published`          |
| Approve          | Status is `Pending Approval`, actor is an approver  | `Approved` or `Published` (if final approver) |
| Reject           | Status is `Pending Approval`, actor is an approver  | `Rejected`           |
| Re-submit        | Status is `Rejected`, actor is the author           | `Pending Approval`   |

---

### 3.2 Approval Workflow (Section 3)

#### 3.2.1 Approver Assignment

- The memo author may assign zero or more approvers when creating or editing a memo.
- Approvers are selected from the internal user database by name or email.
- Approver assignment is optional.
- If no approvers are assigned and the memo is submitted, the system must automatically transition the memo to `Published` status without routing for approval.
- If one or more approvers are assigned, the memo status transitions to `Pending Approval` upon submission.

#### 3.2.2 Notification on Submission

- When a memo transitions to `Pending Approval`, the system must send an email notification to each assigned approver.
- The email must contain:
  - Memo title and memo number.
  - Author name.
  - A direct link to the memo detail page with approve and reject action buttons.
  - A plain-text fallback for email clients that do not render HTML.

#### 3.2.3 Approval Behaviour

- A memo in `Pending Approval` status must not appear in the public "All Documents" list visible to general users.
- The memo must remain accessible to:
  - The memo author via their "My Documents" dashboard tab.
  - Any assigned approver via their "My Approvals" dashboard tab.
  - Any user with a direct URL to the memo (viewing permitted; content access rules apply).
- Each approver's individual decision (Approve or Reject) must be recorded, including timestamp and any optional rejection comment.
- If any approver rejects the memo, the status transitions to `Rejected`.
- If all approvers have approved, the status transitions to `Approved` and then immediately to `Published`.
- For MVP, the approval model is parallel (all approvers are notified simultaneously; any rejection stops the workflow).
- Full sequential/ordered approval chains are deferred to post-MVP.

#### 3.2.4 Approval History

- The memo detail view must display the full approval history, including:
  - Approver name and email.
  - Decision (Approved / Rejected / Pending).
  - Timestamp of decision.
  - Rejection comment (if applicable).

---

### 3.3 User Dashboard (Section 5)

#### 3.3.1 Dashboard Tabs

The dashboard must present three tabs:

| Tab Name        | Content Description                                                                 |
|-----------------|-------------------------------------------------------------------------------------|
| All Documents   | All memos with status `Published`. Excludes drafts and memos pending approval.      |
| My Documents    | All memos created by the currently authenticated user, regardless of status (includes Drafts, Pending Approval, Rejected, Approved, Published). |
| My Approvals    | All memos where the current user is an assigned approver and status is `Pending Approval`. |

#### 3.3.2 List View Columns

Each tab's list view must include the following columns:

| Column      | Description                                        |
|-------------|----------------------------------------------------|
| Memo Number | Auto-generated identifier, sortable                |
| Title       | Memo title, sortable, clickable link to detail view|
| Author      | Display name of the memo author, sortable          |
| Date        | Date created (UTC), sortable                       |
| Status      | Current approval status badge, filterable          |
| Tags        | Comma-separated list of hash tags                  |

#### 3.3.3 Filtering and Sorting

- All columns must support ascending and descending sort toggled by clicking the column header.
- Filter controls must be available for: Status, Date range (from/to), Author, and Tags.
- Filter state must persist for the duration of the user session (page navigation within the app).

#### 3.3.4 Search

**Simple Search:**
- A keyword search input that matches against: Title, Content (plain text), and Tags.
- Case-insensitive matching.
- Results update on submit (Enter key or Search button).

**Advanced Search:**
- Accessible via an expandable panel or modal.
- Filter parameters:
  - Date created (from / to)
  - Author (select from user list)
  - Tag (free text or select)
  - Approval Status (multi-select from enum values)
  - Approver (select from user list)
  - Confidential flag (Yes / No / All) — flag stored and filterable even in MVP

---

### 3.4 Notifications (Section 6)

#### 3.4.1 Email Notifications

The system must send email notifications for the following trigger events:

| Trigger Event                    | Recipient(s)                        | Email Content                                                      |
|----------------------------------|-------------------------------------|--------------------------------------------------------------------|
| Approval request (submission)    | All assigned approvers              | Memo title, number, author, approve/reject action links            |
| Status update — Approved         | Memo author                         | Memo number, title, approver name, timestamp                       |
| Status update — Rejected         | Memo author                         | Memo number, title, approver name, rejection comment, timestamp    |
| Memo published                   | All To and CC distribution list recipients | Memo title, number, author, link to memo                    |

#### 3.4.2 Email Implementation Requirements

- Email delivery must be implemented using MailKit over SMTP.
- SMTP host, port, credentials, and sender address must be configurable via `appsettings.json` (with environment variable override support).
- Email sending must be performed asynchronously and must not block the request/response cycle.
- Failed email delivery must be logged with full error context (recipient, subject, exception). The primary operation (e.g., memo approval) must not be rolled back due to email failure alone.
- Email templates must use plain HTML with an inline-style fallback. Razor-based templates are acceptable.

#### 3.4.3 In-App Notifications (Optional for MVP)

- Real-time in-app alerts are optional for the MVP but the infrastructure must be prepared.
- If implemented: use Blazor Server's SignalR connection for push delivery.
- Notification records must be stored in the database with the fields: recipient user ID, message text, memo reference, read/unread flag, and created timestamp.
- A notification bell icon in the navigation bar must show unread count.
- Clicking a notification must navigate to the relevant memo.

---

## 4. Functional Requirements — Out of Scope for MVP

The following sections are documented here for completeness and to guide post-MVP planning. No implementation work should be undertaken for these areas during the MVP sprint.

### 4.1 Confidential Memo Handling (Section 4)

**Visibility Rules (deferred):**
- When the confidential flag is set, the full content and attachments must only be visible to: memo creator, assigned approvers, To/CC recipients, and system admins.
- All other authenticated users may see metadata (title, author, tags) in "All Documents" but not the content.
- Unauthenticated users see nothing.
- Users who attempt to access restricted content must receive the message: "This is a confidential memo. You do not have permission to view its content."

**Search Behaviour (deferred):**
- Confidential memos must appear in search results with metadata only.
- Content fields must be excluded from search results for unauthorised users.

**MVP Note:** The `Confidential` boolean field is persisted in the database and visible in the memo form during MVP. Enforcement of access control based on this flag is deferred.

### 4.2 Additional Features (Section 7)

| Feature                  | Description                                                                                  |
|--------------------------|----------------------------------------------------------------------------------------------|
| Draft Auto-Save          | Periodic auto-save of memo content while editing to prevent data loss.                       |
| Version Control          | Maintain a full edit history with the ability to view and restore any prior version.         |
| Permissions and Roles    | Formal role definitions: Admin (full access), User (create/edit/approve), Viewer (read-only). Currently all authenticated users are treated as Users in MVP. |
| Export to PDF            | Generate a print-ready PDF rendering of the memo content and metadata.                       |
| Print-Friendly View      | Browser-rendered print stylesheet or dedicated print view.                                   |
| Audit Trail              | Immutable log of all system actions: creation, edits, approvals, views, access denials.      |
| Mobile Responsiveness    | Full responsive layout optimised for smartphone and tablet viewports.                        |

### 4.3 Future Considerations (Section 8)

| Feature                          | Description                                                            |
|----------------------------------|------------------------------------------------------------------------|
| @Mentions                        | Tag users within memo content to trigger notifications.                |
| Tag Suggestions                  | AI-assisted or frequency-based tag suggestions during editing.         |
| Enterprise Calendar Integration  | Link memos to calendar events or external notification systems.        |

---

## 5. Non-Functional Requirements

### 5.1 Performance

- **Page Load Time:** All Blazor Server pages must render interactive content within 3 seconds on a standard corporate LAN connection (10 Mbps+).
- **API Response Time:** All ASP.NET Core API endpoints must respond within 500 ms for read operations and 1000 ms for write operations under normal load (up to 50 concurrent users for MVP).
- **Search Latency:** Simple and advanced search queries must return results within 2 seconds for datasets up to 10,000 memo records.
- **File Upload:** Attachment uploads must provide progress feedback to the user. Single-file uploads up to 10 MB must complete within 30 seconds on a standard connection.
- **Database Queries:** All EF Core queries must use parameterised queries. N+1 query patterns must be avoided; use `.Include()` for required related entities.

### 5.2 Security

- **Authentication:** All non-public routes must require authentication. ASP.NET Core Identity with JWT bearer tokens is used for the Web API layer. Blazor Server uses cookie-based authentication via the Identity system.
- **Authorisation:** Route-level and component-level authorisation attributes must be applied. Users may only edit memos they authored. Approvers may only act on memos they are assigned to.
- **Input Validation:** All user inputs must be validated server-side using Data Annotations and/or FluentValidation. Client-side validation is supplementary only.
- **File Upload Security:** Uploaded file types must be validated against a server-side allowlist. File names must be sanitised. Files must be stored outside the web root.
- **SQL Injection Prevention:** All database interactions must use EF Core parameterised queries. Raw SQL is forbidden unless explicitly reviewed and approved.
- **XSS Prevention:** Rich text content must be sanitised using a trusted HTML sanitiser library (e.g., HtmlSanitizer) before storage and before rendering.
- **CSRF Protection:** ASP.NET Core anti-forgery tokens must be enabled for all forms.
- **Secrets Management:** Database connection strings, SMTP credentials, and JWT signing keys must not be committed to source control. Use `appsettings.json` with environment variable overrides or a secrets manager.
- **HTTPS:** All traffic must be served over HTTPS in production. HTTP must redirect to HTTPS.

### 5.3 Scalability

- **MVP Target Load:** Up to 50 concurrent Blazor Server users.
- **Data Volume:** Designed to handle up to 10,000 memo records without schema changes.
- **Horizontal Scaling Readiness:** Blazor Server's SignalR state is in-process by default. For post-MVP horizontal scaling, SignalR backplane (Azure SignalR Service or Redis) must be adopted. This must be documented as a known limitation.
- **Stateless API Layer:** The ASP.NET Core Web API layer must be designed stateless to permit horizontal scaling without session affinity.
- **Database Connection Pooling:** EF Core must be configured with an appropriate connection pool size via the connection string.

### 5.4 Platform and Deployment

In accordance with the Project Constitution Section 3 (Multi-Platform Deployment):

| Target Platform | Deployment Model                         | Notes                                                                           |
|-----------------|------------------------------------------|---------------------------------------------------------------------------------|
| Windows         | IIS or Kestrel self-hosted on Windows 10+ | Full support. IIS requires the .NET 8 Hosting Bundle.                          |
| Linux           | Kestrel behind nginx/Apache on Ubuntu 22.04+, Debian, Fedora | Docker containerisation recommended. No Windows-specific APIs. |
| macOS           | Kestrel self-hosted for development; not recommended for production | Supported for local development on current and previous major versions. |
| Web Browser     | Blazor Server (SSR + SignalR)            | Chrome 110+, Firefox 110+, Safari 16+, Edge 110+. No WASM download required.   |

**Cross-Platform Guidelines:**
- Use `Path.Combine()` for all file paths; do not hard-code path separators.
- Use `Environment.NewLine` only where appropriate; use `\n` for stored data and network protocols.
- File storage paths must be configurable and not assume Windows drive letters.
- Docker support: a `Dockerfile` and `docker-compose.yml` must be provided targeting `mcr.microsoft.com/dotnet/aspnet:8.0`.

### 5.5 Reliability and Availability

- **Error Handling:** Per Project Constitution Section 5, all exceptions must be caught, logged, and surfaced to the user with a meaningful message. Exceptions must never be silently swallowed.
- **Logging:** Use `Microsoft.Extensions.Logging` with a structured logging provider (Serilog recommended). Log levels: Debug (development), Information (production minimum), Warning, Error, Critical.
- **Database Migrations:** EF Core migrations must be used for all schema changes. The application must apply pending migrations at startup (or via a dedicated migration step in CI/CD).
- **Graceful Degradation:** If the email service is unavailable, memo operations must complete and the failure must be queued for retry or logged prominently.

### 5.6 Maintainability and Code Quality

Per Project Constitution:

- **Clean Architecture:** Strict layer separation. Domain and Application layers must have zero dependencies on Infrastructure or Presentation.
- **Unit Testing:** Every public method must have corresponding unit tests. xUnit is the recommended test framework. Moq is the recommended mocking library. Minimum coverage target: 80% line coverage for Application and Domain layers.
- **Code Comments:** All classes, public methods, and non-trivial logic blocks must include XML documentation comments (`///`) on public members and inline comments for complex logic, written for a beginner-level developer audience.
- **No Speculation:** Recommendations and design decisions must be evidence-based and documented.
- **Truthfulness:** No placeholder or stub implementations may be committed as complete. All stubs must be marked with `// TODO:` and tracked as work items.

---

## 6. Constraints and Assumptions

| ID  | Type        | Statement                                                                                                    |
|-----|-------------|--------------------------------------------------------------------------------------------------------------|
| C1  | Technology  | Backend is .NET 8 / C# / ASP.NET Core. This is a fixed constraint.                                          |
| C2  | Technology  | Frontend is Blazor Server. This is a fixed constraint.                                                       |
| C3  | Database    | SQL Server is the primary target database via EF Core. SQLite may be used for local development.             |
| C4  | Auth        | ASP.NET Core Identity is used for authentication and user management.                                        |
| C5  | Email       | SMTP relay must be available in the deployment environment. No fallback delivery mechanism is in scope.       |
| C6  | Scope       | MVP covers Sections 2, 3, 5, and 6 only. No partial implementation of Sections 4, 7, or 8 is permitted.     |
| A1  | Assumption  | All users are internal employees with a pre-provisioned account. Self-registration is not in scope.          |
| A2  | Assumption  | The deployment environment provides SQL Server 2019+ or Azure SQL.                                           |
| A3  | Assumption  | An SMTP relay (internal or external) is available and reachable from the application server.                 |
| A4  | Assumption  | File attachments are stored on a local or network file system. Cloud blob storage is a post-MVP enhancement. |
| A5  | Assumption  | Approval sequences in the MVP are parallel, not ordered/sequential.                                          |

---

## 7. Acceptance Criteria Summary

The MVP is considered complete when all of the following are demonstrable:

1. An authenticated user can create a new memo with all required metadata fields populated.
2. The memo number is auto-generated in the correct format on first save.
3. A memo with no approvers assigned is automatically published upon submission.
4. A memo with one or more approvers assigned transitions to `Pending Approval` and email notifications are sent to approvers.
5. Each approver can approve or reject the memo via the action links in the email or via the "My Approvals" dashboard tab.
6. Rejection by any approver transitions the memo to `Rejected` and notifies the author.
7. Approval by all approvers transitions the memo to `Published` and notifies the author and distribution list.
8. The User Dashboard displays three tabs: All Documents, My Documents, My Approvals — each with correct data scoping.
9. Memos in the list view can be sorted by all listed columns and filtered by status, date, author, and tags.
10. Simple keyword search returns relevant results from title, content, and tags.
11. Advanced search filters operate correctly for all defined filter parameters.
12. File attachments can be uploaded, stored, and downloaded.
13. All pages and actions require authentication. Unauthenticated requests redirect to the login page.
14. The application runs on Windows and Linux without modification.
15. All Application and Domain layer public methods have unit test coverage.
