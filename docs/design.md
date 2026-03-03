# Corporate Memo System — Architecture and Design Document

**Version:** 1.0
**Date:** 2026-03-02
**Status:** Approved for MVP Development
**Governing Principles:** Project Constitution v1.0 (Clean Architecture, Multi-Platform, Unit Testing)

---

## Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [Technology Stack and Rationale](#2-technology-stack-and-rationale)
3. [Clean Architecture Layers](#3-clean-architecture-layers)
4. [Component Breakdown per Layer](#4-component-breakdown-per-layer)
5. [Database Schema](#5-database-schema)
6. [API Endpoint List](#6-api-endpoint-list)
7. [Project Folder and Solution Structure](#7-project-folder-and-solution-structure)
8. [Cross-Cutting Concerns](#8-cross-cutting-concerns)
9. [Deployment Architecture](#9-deployment-architecture)
10. [Known Limitations and Post-MVP Considerations](#10-known-limitations-and-post-mvp-considerations)

---

## 1. System Architecture Overview

The Corporate Memo System uses a **Clean Architecture** layered design. The system is split into two primary host processes:

1. **Blazor Server Application** — serves the interactive web UI directly to the browser via SignalR. This host also embeds the ASP.NET Core Web API, making the architecture a single deployable unit for the MVP.
2. **SQL Server Database** — the single source of truth for all persistent data.

The following diagram describes the logical layering:

```
+-----------------------------------------------------------+
|                   Presentation Layer                      |
|  Blazor Server UI (Pages, Components, ViewModels)         |
|  ASP.NET Core Web API Controllers                         |
+-----------------------------------------------------------+
              |  calls  |
+-----------------------------------------------------------+
|                   Application Layer                       |
|  Use Cases / Command & Query Handlers (MediatR)           |
|  DTOs / Request & Response Models                         |
|  Interfaces (IEmailService, IMemoRepository, etc.)        |
|  Validation (FluentValidation)                            |
+-----------------------------------------------------------+
              |  calls  |
+-----------------------------------------------------------+
|                    Domain Layer                           |
|  Entities (Memo, User, ApprovalStep, Notification, ...)   |
|  Value Objects                                            |
|  Domain Enums (MemoStatus, etc.)                          |
|  Domain Events                                            |
|  Business Rules (pure C#, no framework dependencies)      |
+-----------------------------------------------------------+
              |  implemented by  |
+-----------------------------------------------------------+
|                 Infrastructure Layer                      |
|  EF Core DbContext + Migrations                           |
|  Repository Implementations                               |
|  Email Service (MailKit/SMTP)                             |
|  File Storage Service                                     |
|  ASP.NET Core Identity Integration                        |
+-----------------------------------------------------------+
```

### Dependency Rule

Per the Project Constitution and Clean Architecture principles:

- Arrows point **inward only**.
- Domain has **zero** external dependencies.
- Application depends only on Domain.
- Infrastructure depends on Application and Domain (for interface implementations).
- Presentation depends on Application (via MediatR commands/queries) and Infrastructure (only for DI registration).

---

## 2. Technology Stack and Rationale

### 2.1 Backend — ASP.NET Core 8 Web API

**Choice:** ASP.NET Core 8 minimal API and controller-based Web API hosted within the same process as Blazor Server.

**Rationale:**
- ASP.NET Core 8 is an LTS release with Microsoft support until November 2026, providing a stable, long-term foundation.
- Native cross-platform support on Windows, Linux, and macOS with Kestrel, directly satisfying the Project Constitution Section 3 requirement.
- First-class dependency injection, middleware pipeline, and built-in support for JWT authentication, routing, and model binding.
- Tight integration with EF Core, Identity, and MailKit reduces third-party surface area.
- MediatR CQRS pattern can be layered in without requiring a separate framework.

**Alternatives considered:**
- **Minimal APIs only (no controllers):** Cleaner for small services but reduces discoverability and makes controller-level attributes (e.g., `[Authorize]`, `[Produces]`) less ergonomic for this scale.
- **gRPC:** Optimal for service-to-service communication but adds complexity for a browser-facing API where REST is more interoperable.

### 2.2 Frontend — Blazor Server

**Choice:** Blazor Server with .NET 8.

**Rationale:**
- Blazor Server renders UI components on the server and streams DOM diffs over SignalR, enabling real-time updates (notification bell, approval status changes) without requiring a separate JavaScript SPA framework.
- Shares C# models and validation logic with the backend, eliminating duplication between client and server code.
- Full access to .NET libraries server-side (no WASM size constraints, no browser sandbox limitations for file operations).
- Supports SSR (Static Server Rendering) in .NET 8's "Blazor United" rendering model, improving initial load performance and SEO where needed.
- Satisfies the browser compatibility requirement (Chrome, Firefox, Safari, Edge) via standard HTML/CSS/SignalR.

**Alternatives considered:**
- **Blazor WebAssembly:** Downloads the .NET runtime to the browser (~10 MB). Longer initial load, and the application cannot easily access server-side resources directly. No advantage for an intranet app where SignalR latency is low.
- **React/Angular SPA + API:** Requires maintaining a separate frontend codebase in TypeScript. Doubles the technology surface area and testing overhead for a team primarily skilled in C#.
- **Razor Pages (MVC):** Simpler but lacks the component model needed for interactive dashboards and real-time notifications without adding manual JavaScript.

### 2.3 Database — SQL Server with EF Core (Code-First)

**Choice:** SQL Server 2019+ (or Azure SQL) as the production database. EF Core 8 with code-first migrations.

**Rationale:**
- SQL Server provides the relational model required for memo entities, approval steps, users, and notification records.
- EF Core 8's code-first approach keeps schema definition in C# entities within the Domain layer, maintaining Clean Architecture's principle of database-agnostic domain models.
- EF Core supports SQL Server, SQLite, and PostgreSQL via provider swap, enabling SQLite for local development without schema changes.
- EF Core 8 includes improved bulk operations and query performance over prior versions.
- Migration scripts are source-controllable and auditable.

**Alternatives considered:**
- **PostgreSQL + Npgsql:** A valid cross-platform alternative. Chosen SQL Server because the requirements assumption (A2) specifies SQL Server / Azure SQL as the target environment.
- **MongoDB:** A document database suits flexible content but weakens relational integrity for approval steps and user assignments. Not appropriate here.
- **Dapper:** Lightweight micro-ORM. Would require hand-written SQL for every query and migration tooling separately. EF Core provides better developer velocity for this scope.

**Development Note:** `appsettings.Development.json` should configure a SQLite provider to allow development without a SQL Server installation.

### 2.4 Email — MailKit over SMTP

**Choice:** MailKit 4.x library for email composition and SMTP delivery.

**Rationale:**
- MailKit is the de facto standard for SMTP in .NET. The `System.Net.Mail.SmtpClient` class is marked as not recommended by Microsoft for new development.
- MailKit supports TLS/SSL, STARTTLS, OAuth2, and modern SMTP authentication — necessary for enterprise SMTP relays.
- Cross-platform: runs identically on Windows, Linux, and macOS.
- MimeKit (bundled with MailKit) provides clean MIME message construction including HTML + plain-text multipart messages.

**Alternatives considered:**
- **SendGrid / Mailgun SDK:** Third-party transactional email APIs. Introduce an external dependency and per-email cost. Not appropriate for an on-premises internal system.
- **System.Net.Mail.SmtpClient:** Deprecated. Lacks async support and modern TLS handling.
- **FluentEmail:** A wrapper around MailKit/Razor templates. Adds a layer of abstraction. Using MailKit directly provides more control with negligible added complexity.

### 2.5 Authentication — ASP.NET Core Identity with JWT

**Choice:** ASP.NET Core Identity for user management and password hashing. JWT bearer tokens for Web API authentication. Cookie authentication for Blazor Server sessions.

**Rationale:**
- ASP.NET Core Identity provides a battle-tested, audited implementation of password hashing (PBKDF2), account lockout, and role management — avoiding the security risks of a custom auth implementation.
- JWT tokens are stateless, enabling the API layer to scale horizontally without shared session storage.
- Blazor Server uses the standard ASP.NET Core cookie authentication scheme, which integrates with Identity out of the box.
- Both JWT and cookie auth can share the same Identity user store, avoiding dual user management.

**Assumptions:**
- User accounts are pre-provisioned by an administrator. Self-registration is not in scope for MVP (see constraint A1).
- LDAP/Active Directory integration is a post-MVP enhancement.

### 2.6 Additional Libraries

| Library         | Version Target | Purpose                                                             |
|-----------------|----------------|---------------------------------------------------------------------|
| MediatR         | 12.x           | CQRS command/query dispatcher within the Application layer         |
| FluentValidation| 11.x           | Command and request validation in the Application layer            |
| HtmlSanitizer   | 8.x            | Server-side sanitisation of rich text content before storage       |
| Serilog         | 3.x            | Structured logging with Console and File sinks                     |
| xUnit           | 2.x            | Unit test framework                                                 |
| Moq             | 4.x            | Mocking framework for unit tests                                   |
| Bogus           | 34.x           | Fake data generation for unit and integration tests                |
| Swashbuckle     | 6.x            | OpenAPI/Swagger documentation for the Web API                      |

---

## 3. Clean Architecture Layers

### 3.1 Domain Layer (`CorporateMemo.Domain`)

The innermost layer. Contains only pure C# with no NuGet package dependencies (except for primitive utilities).

**Responsibilities:**
- Define all business entities as C# classes.
- Define domain enums (e.g., `MemoStatus`).
- Enforce invariants through entity constructors and methods (e.g., a `Memo` cannot transition from `Published` to `Draft`).
- Raise domain events (e.g., `MemoApprovedDomainEvent`).
- Define value objects (e.g., `MemoNumber`).

**Must not contain:**
- EF Core attributes (`[Key]`, `[Column]`, etc.) — these belong in Infrastructure's `IEntityTypeConfiguration<T>` classes.
- Framework-specific code (`IConfiguration`, `HttpContext`, etc.).
- Any I/O operations.

### 3.2 Application Layer (`CorporateMemo.Application`)

The orchestration layer. Coordinates domain objects to fulfil use cases.

**Responsibilities:**
- Define MediatR `IRequest<T>` commands (e.g., `CreateMemoCommand`) and queries (e.g., `GetMemoByIdQuery`).
- Define `IRequestHandler<,>` implementations that contain the use case logic.
- Define repository interfaces (e.g., `IMemoRepository`, `IUserRepository`).
- Define service interfaces (e.g., `IEmailService`, `IFileStorageService`).
- Define DTOs / response models.
- Validate commands using FluentValidation `AbstractValidator<T>`.
- Map between domain entities and DTOs.

**Must not contain:**
- EF Core `DbContext` or any data access implementation.
- SMTP or file system calls.
- Blazor or HTTP-specific code.

**Dependencies:** Domain layer only.

### 3.3 Infrastructure Layer (`CorporateMemo.Infrastructure`)

Implements all external I/O as defined by the Application layer interfaces.

**Responsibilities:**
- `AppDbContext` (EF Core `DbContext` with entity configurations).
- Repository implementations (e.g., `MemoRepository : IMemoRepository`).
- EF Core migrations.
- `EmailService : IEmailService` using MailKit.
- `FileStorageService : IFileStorageService` using the local file system.
- ASP.NET Core Identity user store integration.
- Serilog configuration.

**Dependencies:** Application layer, Domain layer, EF Core, MailKit, ASP.NET Core Identity.

### 3.4 Presentation Layer (`CorporateMemo.Web`)

The outermost layer. The Blazor Server host application and Web API.

**Responsibilities:**
- Blazor Server pages and components (routable pages, layout, reusable components).
- ASP.NET Core Web API controllers.
- `Program.cs` — service registration (DI composition root), middleware pipeline.
- Authentication/authorisation middleware configuration.
- Swagger/OpenAPI configuration.
- `appsettings.json` and environment-specific overrides.

**Dependencies:** Application layer (MediatR commands/queries), Infrastructure layer (for DI registration only).

---

## 4. Component Breakdown per Layer

### 4.1 Domain Layer Components

#### Entities

**`Memo`**
```
Properties:
  Id              : Guid
  MemoNumber      : string           // e.g., "jsmith-20260302-001"
  Title           : string           // max 100 chars
  AuthorId        : Guid
  Author          : ApplicationUser  // navigation
  Content         : string           // sanitised HTML, max 1000 chars plain text equivalent
  Tags            : ICollection<MemoTag>
  IsConfidential  : bool
  Status          : MemoStatus       // enum
  CreatedAt       : DateTime (UTC)
  ApprovedAt      : DateTime? (UTC)
  ApprovalSteps   : ICollection<ApprovalStep>
  Attachments     : ICollection<Attachment>
  ToRecipients    : ICollection<MemoRecipient>
  CcRecipients    : ICollection<MemoRecipient>
  Notifications   : ICollection<Notification>

Methods:
  Submit()               // transitions Draft -> Pending Approval or Published
  Approve(approverId)    // records approval decision
  Reject(approverId, comment) // records rejection decision
  CanEdit(userId)        // returns bool — only author and draft/rejected status
```

**`ApprovalStep`**
```
Properties:
  Id          : Guid
  MemoId      : Guid
  Memo        : Memo
  ApproverId  : Guid
  Approver    : ApplicationUser
  Decision    : ApprovalDecision?    // enum: Approved, Rejected, null = Pending
  Comment     : string?
  DecidedAt   : DateTime? (UTC)
```

**`Attachment`**
```
Properties:
  Id            : Guid
  MemoId        : Guid
  Memo          : Memo
  FileName      : string             // original sanitised file name
  StoredFileName: string             // GUID-based name on disk
  ContentType   : string             // MIME type
  FileSizeBytes : long
  UploadedAt    : DateTime (UTC)
```

**`Notification`**
```
Properties:
  Id          : Guid
  RecipientId : Guid
  Recipient   : ApplicationUser
  MemoId      : Guid?
  Memo        : Memo?
  Message     : string
  IsRead      : bool
  CreatedAt   : DateTime (UTC)
```

**`MemoTag`**
```
Properties:
  Id      : Guid
  MemoId  : Guid
  Memo    : Memo
  Tag     : string
```

**`MemoRecipient`**
```
Properties:
  Id          : Guid
  MemoId      : Guid
  Memo        : Memo
  UserId      : Guid?               // null if external email
  Email       : string
  RecipientType : RecipientType     // enum: To, Cc
```

#### Domain Enums

```csharp
// MemoStatus.cs
public enum MemoStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Published = 4
}

// ApprovalDecision.cs
public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2
}

// RecipientType.cs
public enum RecipientType
{
    To = 1,
    Cc = 2
}
```

#### Domain Events

```
MemoSubmittedDomainEvent      { MemoId, ApproverIds }
MemoApprovedDomainEvent       { MemoId, ApproverId }
MemoRejectedDomainEvent       { MemoId, ApproverId, Comment }
MemoPublishedDomainEvent      { MemoId, RecipientEmails }
```

### 4.2 Application Layer Components

#### Commands

| Command                        | Handler                              | Description                                              |
|-------------------------------|--------------------------------------|----------------------------------------------------------|
| `CreateMemoCommand`           | `CreateMemoCommandHandler`           | Creates a new memo in Draft status                       |
| `UpdateMemoCommand`           | `UpdateMemoCommandHandler`           | Updates title, content, tags, recipients, approvers      |
| `SubmitMemoCommand`           | `SubmitMemoCommandHandler`           | Submits a draft memo for approval or auto-publishes      |
| `ApproveMemoCommand`          | `ApproveMemoCommandHandler`          | Records an approver's approval decision                  |
| `RejectMemoCommand`           | `RejectMemoCommandHandler`           | Records an approver's rejection decision with comment    |
| `UploadAttachmentCommand`     | `UploadAttachmentCommandHandler`     | Stores a file and creates an Attachment record           |
| `DeleteAttachmentCommand`     | `DeleteAttachmentCommandHandler`     | Removes an attachment from storage and the database      |
| `MarkNotificationReadCommand` | `MarkNotificationReadCommandHandler` | Marks a notification as read for the current user        |

#### Queries

| Query                          | Handler                              | Returns                                                  |
|-------------------------------|--------------------------------------|----------------------------------------------------------|
| `GetMemoByIdQuery`            | `GetMemoByIdQueryHandler`            | `MemoDetailDto` or null                                  |
| `GetAllPublishedMemosQuery`   | `GetAllPublishedMemosQueryHandler`   | `PagedResult<MemoSummaryDto>`                            |
| `GetMyMemosQuery`             | `GetMyMemosQueryHandler`             | `PagedResult<MemoSummaryDto>` filtered by author         |
| `GetMyApprovalsQuery`         | `GetMyApprovalsQueryHandler`         | `PagedResult<MemoSummaryDto>` filtered by approver       |
| `SearchMemosQuery`            | `SearchMemosQueryHandler`            | `PagedResult<MemoSummaryDto>` with search/filter params  |
| `GetUsersForSuggestionQuery`  | `GetUsersForSuggestionQueryHandler`  | `IList<UserSuggestionDto>` for autocomplete              |
| `GetUnreadNotificationsQuery` | `GetUnreadNotificationsQueryHandler` | `IList<NotificationDto>` for current user                |

#### Repository Interfaces

```csharp
// IMemoRepository.cs
public interface IMemoRepository
{
    Task<Memo?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Memo>> GetPublishedAsync(MemoSearchParams searchParams, CancellationToken ct = default);
    Task<PagedResult<Memo>> GetByAuthorAsync(Guid authorId, MemoSearchParams searchParams, CancellationToken ct = default);
    Task<PagedResult<Memo>> GetPendingForApproverAsync(Guid approverId, MemoSearchParams searchParams, CancellationToken ct = default);
    Task<string> GenerateMemoNumberAsync(string username, DateTime date, CancellationToken ct = default);
    Task AddAsync(Memo memo, CancellationToken ct = default);
    Task UpdateAsync(Memo memo, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// IUserRepository.cs
public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IList<ApplicationUser>> SearchByEmailOrNameAsync(string query, CancellationToken ct = default);
}

// INotificationRepository.cs
public interface INotificationRepository
{
    Task<IList<Notification>> GetUnreadByUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

#### Service Interfaces

```csharp
// IEmailService.cs
public interface IEmailService
{
    Task SendApprovalRequestAsync(ApprovalRequestEmailDto dto, CancellationToken ct = default);
    Task SendStatusUpdateAsync(StatusUpdateEmailDto dto, CancellationToken ct = default);
    Task SendPublicationNoticeAsync(PublicationNoticeEmailDto dto, CancellationToken ct = default);
}

// IFileStorageService.cs
public interface IFileStorageService
{
    Task<string> SaveAsync(Stream fileStream, string originalFileName, CancellationToken ct = default);
    Task<Stream> ReadAsync(string storedFileName, CancellationToken ct = default);
    Task DeleteAsync(string storedFileName, CancellationToken ct = default);
}

// ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid UserId { get; }
    string UserName { get; }
    string Email { get; }
}
```

#### DTOs

```
MemoSummaryDto    { Id, MemoNumber, Title, AuthorName, CreatedAt, Status, Tags[] }
MemoDetailDto     { ...all MemoSummaryDto fields..., Content, ApprovalSteps[], Attachments[], ToRecipients[], CcRecipients[], IsConfidential, ApprovedAt }
ApprovalStepDto   { ApproverId, ApproverName, ApproverEmail, Decision?, Comment?, DecidedAt? }
AttachmentDto     { Id, FileName, ContentType, FileSizeBytes, UploadedAt }
UserSuggestionDto { Id, DisplayName, Email }
NotificationDto   { Id, Message, MemoId?, MemoTitle?, IsRead, CreatedAt }
PagedResult<T>    { Items, TotalCount, PageNumber, PageSize }
```

### 4.3 Infrastructure Layer Components

#### `AppDbContext`

The EF Core `DbContext`. Configured with:
- `DbSet<Memo>` — memos table.
- `DbSet<ApprovalStep>` — approval steps.
- `DbSet<Attachment>` — file attachment metadata.
- `DbSet<Notification>` — in-app notifications.
- `DbSet<MemoTag>` — memo tags (separate table for queryability).
- `DbSet<MemoRecipient>` — To/CC recipients.
- Identity tables via `IdentityDbContext<ApplicationUser>` base class.

Entity configurations are in separate `IEntityTypeConfiguration<T>` classes (e.g., `MemoConfiguration`, `ApprovalStepConfiguration`) within `Infrastructure/Persistence/Configurations/`.

#### Repository Implementations

Each interface from the Application layer is implemented here using the `AppDbContext`. Example:

```csharp
// MemoRepository.cs — implements IMemoRepository
public class MemoRepository : IMemoRepository
{
    private readonly AppDbContext _context;
    // Constructor injection of AppDbContext
    // All methods use _context with async/await
    // Queries use .Include() to avoid N+1 problems
    // GenerateMemoNumberAsync queries for the count of today's memos for the user and increments
}
```

#### `EmailService`

Implements `IEmailService` using MailKit. Reads SMTP settings from `IOptions<SmtpSettings>`. Composes MimeMessage with both HTML and plain-text parts. Sends asynchronously. All exceptions are caught, logged, and re-thrown as a typed `EmailDeliveryException` to allow callers to handle gracefully.

#### `FileStorageService`

Implements `IFileStorageService`. Stores files in a configurable base directory outside the web root. Generates a GUID-based stored file name to prevent name collisions. Uses `Path.Combine()` for cross-platform path construction.

### 4.4 Presentation Layer Components

#### Blazor Server Pages and Components

| Route                     | Component                    | Description                                                  |
|---------------------------|------------------------------|--------------------------------------------------------------|
| `/`                       | `Index.razor`                | Redirects to `/dashboard`                                    |
| `/login`                  | `Login.razor`                | Login form using Identity cookie auth                        |
| `/dashboard`              | `Dashboard.razor`            | Three-tab dashboard (All Documents, My Documents, My Approvals) |
| `/memos/create`           | `CreateMemo.razor`           | Memo creation form                                           |
| `/memos/{id}/edit`        | `EditMemo.razor`             | Memo editing form (author only, non-published)               |
| `/memos/{id}`             | `MemoDetail.razor`           | Full memo view with approval history and attachments         |
| `/memos/{id}/approve`     | `ApproveMemo.razor`          | Approver action page (approve / reject with comment)         |

**Shared Components:**
```
MemoListComponent          — Reusable list/table used in all three dashboard tabs
MemoFilterBarComponent     — Filter and sort controls
SearchBarComponent         — Simple and advanced search
AttachmentListComponent    — File attachment display and upload
ApprovalHistoryComponent   — Displays approval steps and decisions
NotificationBellComponent  — Unread count badge and dropdown
RichTextEditorComponent    — Wraps a JavaScript-interop rich text editor (e.g., Quill.js via JSInterop)
TagInputComponent          — Chip-based tag entry
UserAutocompleteComponent  — Email/name autocomplete for recipients and approvers
```

#### Web API Controllers

| Controller                | Base Route           | Responsibility                                          |
|---------------------------|----------------------|---------------------------------------------------------|
| `MemosController`         | `/api/memos`         | CRUD operations and workflow actions                    |
| `AttachmentsController`   | `/api/attachments`   | File upload and download                                |
| `UsersController`         | `/api/users`         | User search/suggestions for autocomplete                |
| `NotificationsController` | `/api/notifications` | Retrieve and mark as read                               |

Note: Blazor Server components communicate with the Application layer directly via MediatR (server-side), not via the HTTP API. The HTTP API is exposed for potential future mobile clients or third-party integrations.

---

## 5. Database Schema

### 5.1 Entity Relationship Overview

```
ApplicationUser (Identity)
    |
    |--- (1:N) Memo [as Author]
    |--- (1:N) ApprovalStep [as Approver]
    |--- (1:N) Notification [as Recipient]
    |--- (N:M via MemoRecipient) Memo [as To/CC recipient]

Memo
    |--- (1:N) ApprovalStep
    |--- (1:N) Attachment
    |--- (1:N) MemoTag
    |--- (1:N) MemoRecipient
    |--- (1:N) Notification
```

### 5.2 Table Definitions

#### `AspNetUsers` (ASP.NET Core Identity — extended)

| Column           | Type           | Constraints        | Notes                              |
|------------------|----------------|--------------------|------------------------------------|
| Id               | uniqueidentifier | PK               | Identity default                   |
| UserName         | nvarchar(256)  | NOT NULL, UNIQUE   |                                    |
| NormalizedUserName | nvarchar(256) | NOT NULL, UNIQUE  |                                    |
| Email            | nvarchar(256)  | NOT NULL, UNIQUE   |                                    |
| NormalizedEmail  | nvarchar(256)  | NOT NULL           |                                    |
| DisplayName      | nvarchar(150)  | NOT NULL           | Custom extension field             |
| PasswordHash     | nvarchar(max)  |                    | Identity managed                   |
| SecurityStamp    | nvarchar(max)  |                    | Identity managed                   |
| ConcurrencyStamp | nvarchar(max)  |                    | Identity managed                   |
| LockoutEnd       | datetimeoffset |                    | Identity managed                   |
| LockoutEnabled   | bit            |                    | Identity managed                   |
| AccessFailedCount| int            |                    | Identity managed                   |

#### `Memos`

| Column           | Type             | Constraints                | Notes                                        |
|------------------|------------------|----------------------------|----------------------------------------------|
| Id               | uniqueidentifier | PK                         |                                              |
| MemoNumber       | nvarchar(50)     | NOT NULL, UNIQUE, INDEX    | e.g., `jsmith-20260302-001`                  |
| Title            | nvarchar(100)    | NOT NULL                   |                                              |
| AuthorId         | uniqueidentifier | FK -> AspNetUsers.Id       | NOT NULL, INDEX                              |
| Content          | nvarchar(max)    | NOT NULL                   | Sanitised HTML                               |
| IsConfidential   | bit              | NOT NULL, DEFAULT 0        |                                              |
| Status           | int              | NOT NULL, DEFAULT 0        | MemoStatus enum value                        |
| CreatedAt        | datetime2        | NOT NULL                   | UTC                                          |
| ApprovedAt       | datetime2        | NULL                       | UTC, set on final approval                   |

#### `ApprovalSteps`

| Column       | Type             | Constraints               | Notes                        |
|--------------|------------------|---------------------------|------------------------------|
| Id           | uniqueidentifier | PK                        |                              |
| MemoId       | uniqueidentifier | FK -> Memos.Id, INDEX     | NOT NULL                     |
| ApproverId   | uniqueidentifier | FK -> AspNetUsers.Id      | NOT NULL                     |
| Decision     | int              | NULL                      | NULL = Pending, 1=Approved, 2=Rejected |
| Comment      | nvarchar(500)    | NULL                      | Populated on rejection       |
| DecidedAt    | datetime2        | NULL                      | UTC                          |

#### `Attachments`

| Column           | Type             | Constraints           | Notes                              |
|------------------|------------------|-----------------------|------------------------------------|
| Id               | uniqueidentifier | PK                    |                                    |
| MemoId           | uniqueidentifier | FK -> Memos.Id, INDEX | NOT NULL                           |
| FileName         | nvarchar(260)    | NOT NULL              | Original sanitised file name       |
| StoredFileName   | nvarchar(260)    | NOT NULL, UNIQUE      | GUID-based name on disk            |
| ContentType      | nvarchar(100)    | NOT NULL              | MIME type                          |
| FileSizeBytes    | bigint           | NOT NULL              |                                    |
| UploadedAt       | datetime2        | NOT NULL              | UTC                                |

#### `MemoTags`

| Column  | Type             | Constraints               | Notes                    |
|---------|------------------|---------------------------|--------------------------|
| Id      | uniqueidentifier | PK                        |                          |
| MemoId  | uniqueidentifier | FK -> Memos.Id, INDEX     | NOT NULL                 |
| Tag     | nvarchar(100)    | NOT NULL                  | Lowercased on storage    |

Composite index on `(MemoId, Tag)` to prevent duplicate tags on the same memo.

#### `MemoRecipients`

| Column        | Type             | Constraints               | Notes                          |
|---------------|------------------|---------------------------|--------------------------------|
| Id            | uniqueidentifier | PK                        |                                |
| MemoId        | uniqueidentifier | FK -> Memos.Id, INDEX     | NOT NULL                       |
| UserId        | uniqueidentifier | FK -> AspNetUsers.Id, NULL| NULL if external email         |
| Email         | nvarchar(256)    | NOT NULL                  | Always stored for notification |
| RecipientType | int              | NOT NULL                  | 1=To, 2=Cc                     |

#### `Notifications`

| Column      | Type             | Constraints               | Notes                        |
|-------------|------------------|---------------------------|------------------------------|
| Id          | uniqueidentifier | PK                        |                              |
| RecipientId | uniqueidentifier | FK -> AspNetUsers.Id, INDEX | NOT NULL                   |
| MemoId      | uniqueidentifier | FK -> Memos.Id, NULL      | Nullable for system messages |
| Message     | nvarchar(500)    | NOT NULL                  |                              |
| IsRead      | bit              | NOT NULL, DEFAULT 0       |                              |
| CreatedAt   | datetime2        | NOT NULL                  | UTC                          |

### 5.3 Indexes

| Table           | Index Columns                     | Type    | Purpose                                   |
|-----------------|-----------------------------------|---------|-------------------------------------------|
| Memos           | MemoNumber                        | UNIQUE  | Fast lookup by number                     |
| Memos           | AuthorId                          | INDEX   | My Documents query                        |
| Memos           | Status                            | INDEX   | Status filter queries                     |
| Memos           | CreatedAt                         | INDEX   | Date range queries                        |
| ApprovalSteps   | MemoId                            | INDEX   | Load approval steps for a memo            |
| ApprovalSteps   | ApproverId                        | INDEX   | My Approvals query                        |
| Notifications   | RecipientId, IsRead               | INDEX   | Unread notification count                 |
| MemoTags        | Tag                               | INDEX   | Tag-based search                          |
| MemoTags        | MemoId, Tag                       | UNIQUE  | Prevent duplicate tags on same memo       |

---

## 6. API Endpoint List

All endpoints require authentication (`[Authorize]`) unless noted. Responses use standard HTTP status codes.

### 6.1 Memos

| Method | Route                          | Description                                      | Request Body / Params                        | Response              |
|--------|--------------------------------|--------------------------------------------------|----------------------------------------------|-----------------------|
| GET    | `/api/memos`                   | Get published memos (All Documents tab)          | Query: `page`, `pageSize`, `search`, filters | `PagedResult<MemoSummaryDto>` |
| GET    | `/api/memos/mine`              | Get current user's memos (My Documents tab)      | Query: `page`, `pageSize`, filters           | `PagedResult<MemoSummaryDto>` |
| GET    | `/api/memos/approvals`         | Get pending approvals for current user           | Query: `page`, `pageSize`                    | `PagedResult<MemoSummaryDto>` |
| GET    | `/api/memos/{id}`              | Get full memo detail                             | Path: `id` (Guid)                            | `MemoDetailDto`       |
| POST   | `/api/memos`                   | Create a new memo (Draft)                        | `CreateMemoRequest`                          | `MemoDetailDto` (201) |
| PUT    | `/api/memos/{id}`              | Update a memo (Draft or Rejected state only)     | `UpdateMemoRequest`                          | `MemoDetailDto`       |
| POST   | `/api/memos/{id}/submit`       | Submit memo for approval or auto-publish         | None                                         | `MemoDetailDto`       |
| POST   | `/api/memos/{id}/approve`      | Approver approves the memo                       | `{ comment?: string }`                       | `MemoDetailDto`       |
| POST   | `/api/memos/{id}/reject`       | Approver rejects the memo                        | `{ comment: string }`                        | `MemoDetailDto`       |

### 6.2 Attachments

| Method | Route                                  | Description                    | Request Body / Params | Response            |
|--------|----------------------------------------|--------------------------------|-----------------------|---------------------|
| POST   | `/api/memos/{memoId}/attachments`      | Upload attachment to a memo    | Multipart form data   | `AttachmentDto` (201)|
| GET    | `/api/attachments/{id}/download`       | Download a file                | Path: `id` (Guid)     | File stream         |
| DELETE | `/api/attachments/{id}`                | Delete an attachment           | Path: `id` (Guid)     | 204 No Content      |

### 6.3 Users

| Method | Route               | Description                                  | Request Body / Params    | Response                  |
|--------|---------------------|----------------------------------------------|--------------------------|---------------------------|
| GET    | `/api/users/suggest`| Suggest users by name or email (autocomplete)| Query: `q` (min 2 chars) | `IList<UserSuggestionDto>`|

### 6.4 Notifications

| Method | Route                           | Description                    | Request Body / Params | Response                    |
|--------|---------------------------------|--------------------------------|-----------------------|-----------------------------|
| GET    | `/api/notifications`            | Get unread notifications       | None                  | `IList<NotificationDto>`    |
| PUT    | `/api/notifications/{id}/read`  | Mark notification as read      | Path: `id` (Guid)     | 204 No Content              |

### 6.5 Authentication

| Method | Route              | Description          | Request Body              | Response              |
|--------|--------------------|----------------------|---------------------------|-----------------------|
| POST   | `/api/auth/login`  | Authenticate user    | `{ email, password }`     | `{ token, expiry }`   |
| POST   | `/api/auth/logout` | Invalidate session   | None                      | 200 OK                |

---

## 7. Project Folder and Solution Structure

```
CorporateMemo.sln
│
├── src/
│   ├── CorporateMemo.Domain/                  # Domain Layer — no external NuGet dependencies
│   │   ├── Entities/
│   │   │   ├── Memo.cs
│   │   │   ├── ApprovalStep.cs
│   │   │   ├── Attachment.cs
│   │   │   ├── Notification.cs
│   │   │   ├── MemoTag.cs
│   │   │   └── MemoRecipient.cs
│   │   ├── Enums/
│   │   │   ├── MemoStatus.cs
│   │   │   ├── ApprovalDecision.cs
│   │   │   └── RecipientType.cs
│   │   ├── Events/
│   │   │   ├── MemoSubmittedDomainEvent.cs
│   │   │   ├── MemoApprovedDomainEvent.cs
│   │   │   ├── MemoRejectedDomainEvent.cs
│   │   │   └── MemoPublishedDomainEvent.cs
│   │   └── Exceptions/
│   │       ├── MemoNotFoundException.cs
│   │       └── InvalidMemoTransitionException.cs
│   │
│   ├── CorporateMemo.Application/             # Application Layer — depends on Domain only
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IMemoRepository.cs
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── INotificationRepository.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   ├── IFileStorageService.cs
│   │   │   │   └── ICurrentUserService.cs
│   │   │   ├── Models/
│   │   │   │   ├── PagedResult.cs
│   │   │   │   └── MemoSearchParams.cs
│   │   │   └── Behaviours/
│   │   │       ├── ValidationBehaviour.cs     # MediatR pipeline validation
│   │   │       └── LoggingBehaviour.cs        # MediatR pipeline logging
│   │   ├── DTOs/
│   │   │   ├── MemoSummaryDto.cs
│   │   │   ├── MemoDetailDto.cs
│   │   │   ├── ApprovalStepDto.cs
│   │   │   ├── AttachmentDto.cs
│   │   │   ├── UserSuggestionDto.cs
│   │   │   └── NotificationDto.cs
│   │   ├── Memos/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateMemo/
│   │   │   │   │   ├── CreateMemoCommand.cs
│   │   │   │   │   ├── CreateMemoCommandHandler.cs
│   │   │   │   │   └── CreateMemoCommandValidator.cs
│   │   │   │   ├── UpdateMemo/
│   │   │   │   │   ├── UpdateMemoCommand.cs
│   │   │   │   │   ├── UpdateMemoCommandHandler.cs
│   │   │   │   │   └── UpdateMemoCommandValidator.cs
│   │   │   │   ├── SubmitMemo/
│   │   │   │   │   ├── SubmitMemoCommand.cs
│   │   │   │   │   └── SubmitMemoCommandHandler.cs
│   │   │   │   ├── ApproveMemo/
│   │   │   │   │   ├── ApproveMemoCommand.cs
│   │   │   │   │   └── ApproveMemoCommandHandler.cs
│   │   │   │   └── RejectMemo/
│   │   │   │       ├── RejectMemoCommand.cs
│   │   │   │       └── RejectMemoCommandHandler.cs
│   │   │   └── Queries/
│   │   │       ├── GetMemoById/
│   │   │       │   ├── GetMemoByIdQuery.cs
│   │   │       │   └── GetMemoByIdQueryHandler.cs
│   │   │       ├── GetAllPublishedMemos/
│   │   │       │   ├── GetAllPublishedMemosQuery.cs
│   │   │       │   └── GetAllPublishedMemosQueryHandler.cs
│   │   │       ├── GetMyMemos/
│   │   │       │   ├── GetMyMemosQuery.cs
│   │   │       │   └── GetMyMemosQueryHandler.cs
│   │   │       ├── GetMyApprovals/
│   │   │       │   ├── GetMyApprovalsQuery.cs
│   │   │       │   └── GetMyApprovalsQueryHandler.cs
│   │   │       └── SearchMemos/
│   │   │           ├── SearchMemosQuery.cs
│   │   │           └── SearchMemosQueryHandler.cs
│   │   ├── Attachments/
│   │   │   └── Commands/
│   │   │       ├── UploadAttachment/
│   │   │       └── DeleteAttachment/
│   │   └── Notifications/
│   │       ├── Commands/
│   │       │   └── MarkNotificationRead/
│   │       └── Queries/
│   │           └── GetUnreadNotifications/
│   │
│   ├── CorporateMemo.Infrastructure/          # Infrastructure Layer — EF Core, MailKit, file system
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── MemoConfiguration.cs
│   │   │   │   ├── ApprovalStepConfiguration.cs
│   │   │   │   ├── AttachmentConfiguration.cs
│   │   │   │   ├── NotificationConfiguration.cs
│   │   │   │   ├── MemoTagConfiguration.cs
│   │   │   │   └── MemoRecipientConfiguration.cs
│   │   │   ├── Migrations/                    # EF Core auto-generated migrations
│   │   │   └── Repositories/
│   │   │       ├── MemoRepository.cs
│   │   │       ├── UserRepository.cs
│   │   │       └── NotificationRepository.cs
│   │   ├── Email/
│   │   │   ├── EmailService.cs
│   │   │   ├── SmtpSettings.cs               # Configuration POCO
│   │   │   └── Templates/
│   │   │       ├── ApprovalRequest.html
│   │   │       ├── StatusUpdate.html
│   │   │       └── PublicationNotice.html
│   │   ├── FileStorage/
│   │   │   ├── FileStorageService.cs
│   │   │   └── FileStorageSettings.cs
│   │   ├── Identity/
│   │   │   └── ApplicationUser.cs            # IdentityUser<Guid> extension
│   │   └── DependencyInjection.cs            # Extension method: AddInfrastructure(IServiceCollection)
│   │
│   └── CorporateMemo.Web/                    # Presentation Layer — Blazor Server + Web API
│       ├── Program.cs                         # DI composition root, middleware pipeline
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       │   ├── MemosController.cs
│       │   ├── AttachmentsController.cs
│       │   ├── UsersController.cs
│       │   ├── NotificationsController.cs
│       │   └── AuthController.cs
│       ├── Components/
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── NavMenu.razor
│       │   ├── Pages/
│       │   │   ├── Index.razor
│       │   │   ├── Login.razor
│       │   │   ├── Dashboard.razor
│       │   │   ├── CreateMemo.razor
│       │   │   ├── EditMemo.razor
│       │   │   ├── MemoDetail.razor
│       │   │   └── ApproveMemo.razor
│       │   └── Shared/
│       │       ├── MemoListComponent.razor
│       │       ├── MemoFilterBarComponent.razor
│       │       ├── SearchBarComponent.razor
│       │       ├── AttachmentListComponent.razor
│       │       ├── ApprovalHistoryComponent.razor
│       │       ├── NotificationBellComponent.razor
│       │       ├── RichTextEditorComponent.razor
│       │       ├── TagInputComponent.razor
│       │       └── UserAutocompleteComponent.razor
│       ├── Services/
│       │   └── CurrentUserService.cs          # ICurrentUserService implementation
│       └── wwwroot/
│           ├── css/
│           ├── js/
│           │   └── richTextInterop.js         # Quill.js integration for JS interop
│           └── favicon.ico
│
├── tests/
│   ├── CorporateMemo.Domain.Tests/
│   │   └── Entities/
│   │       ├── MemoTests.cs
│   │       └── ApprovalStepTests.cs
│   ├── CorporateMemo.Application.Tests/
│   │   ├── Memos/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateMemoCommandHandlerTests.cs
│   │   │   │   ├── SubmitMemoCommandHandlerTests.cs
│   │   │   │   ├── ApproveMemoCommandHandlerTests.cs
│   │   │   │   └── RejectMemoCommandHandlerTests.cs
│   │   │   └── Queries/
│   │   │       ├── GetMemoByIdQueryHandlerTests.cs
│   │   │       └── SearchMemosQueryHandlerTests.cs
│   │   └── Notifications/
│   │       └── GetUnreadNotificationsQueryHandlerTests.cs
│   └── CorporateMemo.Infrastructure.Tests/
│       ├── Repositories/
│       │   └── MemoRepositoryTests.cs         # Uses in-memory or SQLite test DB
│       └── Email/
│           └── EmailServiceTests.cs           # Uses mock SMTP
│
├── Dockerfile
├── docker-compose.yml
└── README.md
```

---

## 8. Cross-Cutting Concerns

### 8.1 Logging

- **Library:** Serilog with `Microsoft.Extensions.Logging` integration.
- **Sinks:** Console (structured JSON in production), File (rolling daily, retained 7 days).
- **Log levels by environment:**
  - Development: `Debug` and above.
  - Production: `Information` and above, `Error` to a separate error log file.
- **Structured properties on every log entry:** `MemoId`, `UserId`, `Action`, `RequestId`.
- **MediatR Logging Behaviour:** `LoggingBehaviour<TRequest, TResponse>` logs every command/query with duration in milliseconds.

### 8.2 Error Handling

- Global exception handler middleware in `Program.cs` catches unhandled exceptions and returns a structured `ProblemDetails` JSON response (RFC 7807).
- Blazor Server uses an `<ErrorBoundary>` component wrapping all page content to display a user-friendly error message without crashing the circuit.
- Domain exceptions (`MemoNotFoundException`, `InvalidMemoTransitionException`) are mapped to HTTP 404 and HTTP 422 respectively by the exception handler.
- Validation failures from FluentValidation (via `ValidationBehaviour`) are mapped to HTTP 400 with a list of field errors.

### 8.3 Validation Pipeline

MediatR pipeline behaviour `ValidationBehaviour<TRequest, TResponse>`:

1. Receives a command or query.
2. Looks up all registered `IValidator<TRequest>` instances.
3. Runs all validators.
4. If any failures exist, throws `ValidationException` with all failure details.
5. `ValidationException` is caught by the global error handler and returned as HTTP 400.

### 8.4 Security Headers

ASP.NET Core middleware in `Program.cs` must add:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Content-Security-Policy` appropriate for Blazor Server (allowing `wss:` for SignalR).
- `Referrer-Policy: strict-origin-when-cross-origin`

### 8.5 Configuration Structure (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=CorporateMemo;Trusted_Connection=true;"
  },
  "Smtp": {
    "Host": "smtp.company.internal",
    "Port": 587,
    "UseSsl": true,
    "Username": "",
    "Password": "",
    "SenderName": "Corporate Memo System",
    "SenderEmail": "noreply@company.internal"
  },
  "FileStorage": {
    "BasePath": "/var/app/uploads",
    "MaxFileSizeBytes": 10485760,
    "MaxTotalSizeBytes": 52428800,
    "AllowedExtensions": [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg", ".gif", ".bmp"]
  },
  "Jwt": {
    "Key": "",
    "Issuer": "CorporateMemo",
    "Audience": "CorporateMemoUsers",
    "ExpiryMinutes": 480
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

All sensitive values (`Smtp:Password`, `Jwt:Key`, connection string credentials) must be provided via environment variables or a secrets manager and must not be committed to source control.

---

## 9. Deployment Architecture

### 9.1 Single-Host MVP Deployment

For the MVP, the application is deployed as a single ASP.NET Core process hosting both Blazor Server and the Web API on the same port.

```
[Browser]
    |  HTTPS (443)
    v
[Reverse Proxy: nginx / IIS / Kestrel direct]
    |
    v
[CorporateMemo.Web — Kestrel — ASP.NET Core 8]
    |                    |
    v                    v
[SQL Server]     [File System: /uploads]
```

### 9.2 Docker Deployment (Linux)

```dockerfile
# Dockerfile (multi-stage build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/CorporateMemo.Web/CorporateMemo.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CorporateMemo.Web.dll"]
```

`docker-compose.yml` provides SQL Server and the application as services with a shared network.

---

## 10. Known Limitations and Post-MVP Considerations

| ID  | Limitation                                              | Post-MVP Recommendation                                              |
|-----|---------------------------------------------------------|----------------------------------------------------------------------|
| L1  | Blazor Server requires persistent SignalR connection per user; does not scale horizontally without a backplane | Add Azure SignalR Service or Redis backplane for horizontal scaling |
| L2  | File attachments stored on local file system; single-server only | Migrate to Azure Blob Storage or S3-compatible object storage      |
| L3  | Approval model is parallel only (any rejection stops workflow) | Implement sequential/ordered approval chains as a post-MVP feature |
| L4  | No LDAP/AD integration; users must be manually provisioned | Integrate ASP.NET Core Identity with OpenID Connect / LDAP         |
| L5  | No email retry mechanism; failed emails are only logged | Implement a background job queue (Hangfire or similar) for email retries |
| L6  | Confidential memo access control is not enforced in MVP | Implement visibility rules per Section 4 in post-MVP sprint         |
| L7  | No audit trail beyond approval history                  | Implement immutable audit log (Section 7.5) post-MVP               |
| L8  | Rich text editor requires Quill.js JavaScript interop   | Evaluate BlazorRichTextEditor or Radzen alternatives for better integration |
