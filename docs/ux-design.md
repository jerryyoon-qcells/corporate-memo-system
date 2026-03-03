# Corporate Memo System — UX Design Document

**Version:** 1.0
**Date:** 2026-03-02
**Status:** Draft
**Technology Stack:** Blazor Server / Bootstrap 5 / .NET 8
**Governing Documents:** requirements.md, design.md, work-items.md

---

## Table of Contents

1. [Design Principles](#1-design-principles)
2. [Visual Language](#2-visual-language)
   - 2.1 [Color Palette](#21-color-palette)
   - 2.2 [Typography](#22-typography)
   - 2.3 [Spacing and Grid](#23-spacing-and-grid)
3. [User Flow Diagrams](#3-user-flow-diagrams)
   - 3.1 [Create and Submit Memo Flow](#31-create-and-submit-memo-flow)
   - 3.2 [Approval Flow](#32-approval-flow)
   - 3.3 [Dashboard Navigation Flow](#33-dashboard-navigation-flow)
4. [Screen Designs](#4-screen-designs)
   - 4.1 [Login Page](#41-login-page)
   - 4.2 [Main Layout / Shell](#42-main-layout--shell)
   - 4.3 [Dashboard — All Documents Tab](#43-dashboard--all-documents-tab)
   - 4.4 [Dashboard — My Documents Tab](#44-dashboard--my-documents-tab)
   - 4.5 [Dashboard — My Approvals Tab](#45-dashboard--my-approvals-tab)
   - 4.6 [Memo Create / Edit Page](#46-memo-create--edit-page)
   - 4.7 [Memo Detail / View Page](#47-memo-detail--view-page)
   - 4.8 [Approve / Reject Modal](#48-approve--reject-modal)
   - 4.9 [Notification Panel](#49-notification-panel)
   - 4.10 [Admin Panel](#410-admin-panel)

---

## 1. Design Principles

The following five principles govern every design decision in this document. Each principle maps directly to a user need or system constraint identified in the requirements.

### 1. Clarity

Every screen must communicate its purpose within three seconds of viewing. Labels, headings, and status indicators must use plain language. Abbreviations are forbidden unless universally understood (e.g., "CC"). Status values (Draft, Pending Approval, Approved, Rejected, Published) must always be visible, never hidden in a tooltip or secondary view. Error messages must state what went wrong and what the user should do next — never generic messages like "An error occurred."

### 2. Efficiency

Frequent actions (Save Draft, Submit, View) must be reachable in one or two interactions. The dashboard is the home screen; returning to it must always be one click away. Search results must appear without a full page reload. Form fields must use sensible defaults and auto-populate where possible (Author, Date, Memo Number). Keyboard navigation must be supported throughout.

### 3. Feedback

Every action must produce an immediate visual response. Buttons show a loading spinner during async operations. Form submissions show inline validation errors before the server round-trip where possible. Toast notifications confirm successful saves, submissions, approvals, and rejections. Destructive actions (Delete, Reject) require a confirmation step. File uploads show progress bars.

### 4. Consistency

The same component is used for the same purpose everywhere. Status badges use exactly one color scheme throughout the application (see Color Palette). All data tables share the same column header, sort arrow, pagination, and empty-state patterns. Action buttons follow a fixed priority order: primary action left, secondary action right, destructive action last with danger styling. Navigation structure never changes between pages.

### 5. Accessibility

The application must meet WCAG 2.1 Level AA. All interactive elements must be reachable by keyboard (Tab / Shift+Tab / Enter / Space). Color must never be the sole differentiator of meaning — status badges include text labels alongside color. Form inputs must have associated `<label>` elements. Images and icons used as controls must have `aria-label` attributes. Contrast ratio must meet 4.5:1 for normal text and 3:1 for large text. Bootstrap 5's built-in accessibility attributes (`aria-*`, `role`) must be used consistently.

---

## 2. Visual Language

### 2.1 Color Palette

Bootstrap 5 semantic color tokens are mapped to application-specific meaning as follows. These mappings must be applied consistently — never use a color for a purpose other than its defined meaning.

| Token             | Hex Approx.  | Bootstrap Class Prefix | Application Meaning                                    |
|-------------------|-------------|------------------------|--------------------------------------------------------|
| Primary (Blue)    | #0d6efd     | `btn-primary`, `text-primary`, `bg-primary` | Call-to-action buttons, active nav links, links |
| Success (Green)   | #198754     | `btn-success`, `badge bg-success` | Approved status, positive confirmations       |
| Warning (Yellow)  | #ffc107     | `btn-warning`, `badge bg-warning` | Pending Approval status, caution indicators   |
| Danger (Red)      | #dc3545     | `btn-danger`, `badge bg-danger`   | Rejected status, delete actions, error states |
| Secondary (Gray)  | #6c757d     | `btn-secondary`, `badge bg-secondary` | Draft status, disabled controls, muted text |
| Info (Cyan-Blue)  | #0dcaf0     | `btn-info`, `badge bg-info`       | Published status, informational alerts        |
| Light             | #f8f9fa     | `bg-light`                        | Page backgrounds, sidebar background         |
| Dark              | #212529     | `text-dark`, `bg-dark`            | Primary text, top navigation bar background  |
| White             | #ffffff     | `bg-white`                        | Card backgrounds, modal backgrounds          |
| Border            | #dee2e6     | `border`                          | Table borders, card borders, dividers        |

**Status Badge Summary:**

```
  Draft            Pending Approval      Approved         Rejected         Published
 ┌──────────┐      ┌──────────────────┐  ┌──────────┐    ┌──────────┐    ┌───────────┐
 │  Draft   │      │ Pending Approval  │  │ Approved │    │ Rejected │    │ Published │
 │  (gray)  │      │    (yellow)       │  │  (green) │    │  (red)   │    │  (blue)   │
 └──────────┘      └──────────────────┘  └──────────┘    └──────────┘    └───────────┘
 bg-secondary      bg-warning text-dark  bg-success       bg-danger        bg-info
```

### 2.2 Typography

Bootstrap 5 uses a native system font stack by default. No custom web fonts are required for the MVP.

| Element            | Bootstrap Style                          | Usage                                      |
|--------------------|------------------------------------------|--------------------------------------------|
| Page Title (h1)    | `fs-2 fw-bold text-dark`                 | Page-level headings (e.g., "Dashboard")    |
| Section Title (h2) | `fs-4 fw-semibold text-dark`             | Card headers, modal titles                 |
| Sub-heading (h3)   | `fs-5 fw-semibold text-dark`             | Table section headers, panel labels        |
| Body Text          | `fs-6` (16px base)                       | All paragraph and label text               |
| Small / Caption    | `small text-muted` (approx 14px)         | Meta information, timestamps, help text    |
| Memo Number        | `font-monospace fw-semibold text-dark`   | Memo number display throughout the app     |
| Code               | `<code>` element                         | Not used in UI; reserved for technical views |

**Line Length:** Content reading areas (memo body) are constrained to a maximum of 80ch (characters) to maintain readability.

**Heading Hierarchy:** Only one `h1` per page. Modal titles use `h5` per Bootstrap modal conventions.

### 2.3 Spacing and Grid

Bootstrap 5's 12-column grid and spacing scale (rem-based, 0.25rem increments) are used throughout.

| Spacing Token | Value    | Bootstrap Class | Typical Use                                        |
|---------------|----------|-----------------|----------------------------------------------------|
| 1             | 0.25rem  | `m-1`, `p-1`   | Tight internal padding (badge inner padding)       |
| 2             | 0.5rem   | `m-2`, `p-2`   | Button padding, chip/tag padding                   |
| 3             | 1rem     | `m-3`, `p-3`   | Card internal padding, form group spacing          |
| 4             | 1.5rem   | `m-4`, `p-4`   | Section spacing, modal body padding                |
| 5             | 3rem     | `m-5`, `p-5`   | Page-level section separation                      |

**Grid Breakpoints (Bootstrap 5 defaults):**

| Breakpoint | Abbreviation | Min Width | Layout Behavior                                         |
|------------|-------------|-----------|----------------------------------------------------------|
| Extra small | xs         | < 576px   | Single column; sidebar collapses to off-canvas          |
| Small       | sm         | >= 576px  | Single column; condensed table columns                  |
| Medium      | md         | >= 768px  | Two-column layout begins on Memo Create/Edit            |
| Large       | lg         | >= 992px  | Full layout; sidebar visible; all columns shown         |
| Extra large | xl         | >= 1200px | Optimal reading width; content area max-width applied   |

**Container:** `container-fluid` for the shell layout; `container` (max-width 1320px on xl) for content areas.

**Sidebar Width:** Fixed at 240px on lg+. Collapses to 0px (off-canvas) on md and below, toggled by a hamburger button.

---

## 3. User Flow Diagrams

### 3.1 Create and Submit Memo Flow

```
  [User clicks "+ New Memo"]
           │
           ▼
  ┌─────────────────────────┐
  │   Memo Create Page      │
  │  (all fields blank)     │
  │  Memo# = "Auto-assigned"│
  └─────────────────────────┘
           │
           │  User fills in Title, Content,
           │  Tags, Recipients, Approvers
           │
     ┌─────┴──────┐
     │            │
     ▼            ▼
[Save Draft]  [Submit]
     │            │
     ▼            ▼
  Status=     Approvers
  Draft       assigned?
     │         │       │
     │        YES      NO
     │         │       │
     │         ▼       ▼
     │    Status=    Status=
     │    Pending    Published
     │    Approval       │
     │         │         │
     │         │  Email sent to
     │         │  distribution list
     │         │         │
     ▼         ▼         ▼
  ┌──────────────────────────────┐
  │     Memo Detail Page         │
  │  (read-only, shows status)   │
  └──────────────────────────────┘
           │
     (User edits Draft/Rejected)
           │
           ▼
  ┌──────────────────────┐
  │  Memo Edit Page      │
  │  (pre-filled fields) │
  └──────────────────────┘
```

### 3.2 Approval Flow

```
  Memo in "Pending Approval" status
           │
           │  Email sent to all approvers simultaneously (parallel)
           │
     ┌─────┴───────────────────────────────────┐
     │                                         │
  Approver A                             Approver B
  clicks link / opens My Approvals       (same flow)
     │
     ▼
  ┌───────────────────────┐
  │  Approve/Reject Modal │
  │  (optional comment)   │
  └───────────────────────┘
     │
  ┌──┴──┐
  │     │
[Approve] [Reject]
  │         │
  │         ▼
  │    Status = Rejected
  │    Email sent to Author
  │    (with rejection comment)
  │         │
  ▼         ▼
All       End of workflow
approvers  Author sees
approved?  "Rejected" in
  │        My Documents
  │
 YES
  │
  ▼
Status = Approved → Published
Email sent to Author
Email sent to To/CC recipients
Memo appears in "All Documents"
```

### 3.3 Dashboard Navigation Flow

```
  ┌───────────────────────────────────────────────────────┐
  │                    Login Page                         │
  └─────────────────────────┬─────────────────────────────┘
                            │ Successful login
                            ▼
  ┌───────────────────────────────────────────────────────┐
  │              Dashboard (All Documents tab)            │
  │  ┌──────────────┬─────────────────┬────────────────┐  │
  │  │ All Documents│  My Documents   │  My Approvals  │  │
  │  └──────────────┴─────────────────┴────────────────┘  │
  └───────────────────────────────────────────────────────┘
          │                 │                   │
          │                 │                   │
          ▼                 ▼                   ▼
    Click memo row    Click [Edit]         Click [Approve]
          │           on Draft memo        or [Reject]
          ▼                 │                   │
  ┌──────────────┐   ┌──────────────┐    ┌──────────────────┐
  │  Memo Detail │   │  Memo Edit   │    │ Approve/Reject   │
  │  (read-only) │   │  Page        │    │ Modal            │
  └──────────────┘   └──────────────┘    └──────────────────┘
          │                 │                   │
  Context actions:    [Save Draft]        Confirmation
  [Edit]/[Recall]/    [Submit]            → Toast notification
  [Approve]/[Reject]/ [Cancel]           → Table row updates
  [Export PDF]              │
                     ┌──────┴──────┐
                     │             │
              Memo Detail    Dashboard
              (after save)   (on cancel)
```

---

## 4. Screen Designs

---

### 4.1 Login Page

#### Purpose

The login page is the entry point for all users. It authenticates the user against ASP.NET Core Identity using email and password, establishes a cookie-based session, and redirects to the Dashboard on success.

#### User Stories Served

- E0-S3: Authentication — ASP.NET Core Identity and Login UI

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────┐
│                         (browser window)                            │
│                                                                     │
│                                                                     │
│              ┌───────────────────────────────────┐                 │
│              │                                   │                 │
│              │     ┌─────────────────────┐       │                 │
│              │     │   [COMPANY LOGO]    │       │                 │
│              │     │   120px × 60px      │       │                 │
│              │     └─────────────────────┘       │                 │
│              │                                   │                 │
│              │    Corporate Memo System          │                 │
│              │    ─────────────────────────      │                 │
│              │    Sign in to your account        │                 │
│              │                                   │                 │
│              │   ┌───────────────────────────┐   │                 │
│              │   │ Email address             │   │                 │
│              │   │ ┌─────────────────────┐  │   │                 │
│              │   │ │ john.smith@corp.com  │  │   │                 │
│              │   │ └─────────────────────┘  │   │                 │
│              │   └───────────────────────────┘   │                 │
│              │                                   │                 │
│              │   ┌───────────────────────────┐   │                 │
│              │   │ Password                  │   │                 │
│              │   │ ┌─────────────────────┐  │   │                 │
│              │   │ │ ••••••••••••        │  │   │                 │
│              │   │ └─────────────────────┘  │   │                 │
│              │   └───────────────────────────┘   │                 │
│              │                                   │                 │
│              │   ┌───────────────────────────┐   │                 │
│              │   │ [x] Remember me           │   │                 │
│              │   └───────────────────────────┘   │                 │
│              │                                   │                 │
│              │   ┌───────────────────────────┐   │                 │
│              │   │  [    Sign In    ]         │   │  ← btn-primary  │
│              │   └───────────────────────────┘   │                 │
│              │                                   │                 │
│              │   Forgot your password?           │                 │
│              │   Contact your administrator.     │                 │
│              │                                   │                 │
│              │   ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   │                 │
│              │                                   │                 │
│              │  ⚠  Invalid email or password.    │  ← (error state │
│              │     Please try again.             │     alert-danger)│
│              │                                   │                 │
│              └───────────────────────────────────┘                 │
│                    Card: shadow-sm, rounded-3                       │
│                    Width: 420px, centered with flex                 │
│                    Page bg: bg-light                                │
│                                                                     │
│  ─────────────────────────────────────────────────────────────────  │
│  Corporate Memo System v1.0  |  © 2026 Your Company                │
└─────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component              | Bootstrap / HTML Element          | Notes                                      |
|------------------------|-----------------------------------|--------------------------------------------|
| Page wrapper           | `d-flex justify-content-center align-items-center min-vh-100 bg-light` | Centers card vertically and horizontally |
| Login card             | `card shadow-sm rounded-3 p-4`    | Fixed width 420px                          |
| Company logo           | `<img>` inside card header        | Placeholder: 120×60px, `alt="Company Logo"` |
| App title              | `h4 text-center fw-bold mb-1`     | "Corporate Memo System"                    |
| Sub-heading            | `p text-center text-muted mb-4`   | "Sign in to your account"                 |
| Email input            | `<InputText>` / `form-control`    | `type="email"`, `autocomplete="email"`     |
| Password input         | `<InputText>` / `form-control`    | `type="password"`, `autocomplete="current-password"` |
| Remember me            | `<InputCheckbox>` / `form-check`  | Optional; stores persistent cookie        |
| Sign In button         | `btn btn-primary w-100`           | Full-width; shows spinner while loading    |
| Forgot password        | Plain text + admin contact        | No self-service reset in MVP (constraint A1) |
| Error alert            | `alert alert-danger` (conditional)| Shown only on failed login attempt         |
| Footer                 | `text-muted small text-center`    | Version and copyright                      |

#### Interaction Notes

- **Loading state:** The Sign In button displays a `spinner-border spinner-border-sm` and "Signing in..." text while the authentication request is in flight. The button is disabled to prevent double submission.
- **Error state:** On failed authentication, an `alert alert-danger` appears above the Sign In button with the message: "Invalid email or password. Please try again." The password field is cleared; focus returns to it.
- **Success:** On successful login, the browser is redirected to `/dashboard` via Blazor NavigationManager. The redirect uses `forceLoad: false` to preserve the SignalR circuit.
- **Validation:** Both fields show inline `invalid-feedback` messages if submitted empty: "Email address is required." / "Password is required."
- **Forgot password:** No self-service password reset exists in MVP (constraint A1 — all accounts are admin-provisioned). The text reads: "Forgot your password? Contact your administrator."
- **Responsive:** Card reduces to full-width with `mx-3` margin on xs/sm viewports.
- **Accessibility:** `<label for="...">` on all inputs. `autofocus` on email field. `aria-describedby` links error alert to the form.

---

### 4.2 Main Layout / Shell

#### Purpose

The main layout provides persistent chrome — top navigation bar, collapsible left sidebar, and footer — that wraps every authenticated page. It gives users consistent access to navigation, search, notifications, and their account.

#### User Stories Served

- E0-S6: Navigation Shell and Layout

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TOP NAVIGATION BAR  (bg-dark, fixed-top, height: 56px)                │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ [≡]  [LOGO] Corporate Memo     [🔍 Search memos...      ] [🔔 3] [👤▾]│ │
│ │  ↑     ↑         ↑                      ↑                  ↑      ↑  │ │
│ │sidebar logo   app title          global search        notif  avatar│ │
│ │toggle  img    h6 fw-bold         input (form-control)  bell  dropdown│ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│ ┌────────────────┐ ┌───────────────────────────────────────────────┐   │
│ │  LEFT SIDEBAR  │ │            MAIN CONTENT AREA                  │   │
│ │  (240px, bg-   │ │                                               │   │
│ │  white, border-│ │   < Router outlet — page content renders here>│   │
│ │  end)          │ │                                               │   │
│ │                │ │                                               │   │
│ │ NAVIGATION     │ │                                               │   │
│ │ ─────────────  │ │                                               │   │
│ │ 📄 Dashboard   │ │                                               │   │
│ │    ↳ All Docs  │ │                                               │   │
│ │    ↳ My Docs   │ │                                               │   │
│ │    ↳ My Apprvls│ │                                               │   │
│ │                │ │                                               │   │
│ │ + New Memo     │ │                                               │   │
│ │                │ │                                               │   │
│ │ ─────────────  │ │                                               │   │
│ │ ADMIN          │ │                                               │   │
│ │ (only if admin)│ │                                               │   │
│ │ 👤 Users       │ │                                               │   │
│ │ ⚙  Settings    │ │                                               │   │
│ │                │ │                                               │   │
│ │                │ │                                               │   │
│ │                │ │                                               │   │
│ │                │ │                                               │   │
│ └────────────────┘ └───────────────────────────────────────────────┘   │
│                                                                         │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │  FOOTER  (bg-light, border-top, py-2)                               │ │
│ │  Corporate Memo System v1.0  |  © 2026 Your Company. All rights     │ │
│ │  reserved.                                                          │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘

USER AVATAR DROPDOWN (shown when [👤▾] is clicked):
┌──────────────────┐
│  John Smith      │
│  john@corp.com   │
│ ──────────────── │
│  👤 Profile      │
│  🚪 Logout       │
└──────────────────┘

NOTIFICATION BELL BADGE:
  [🔔]  ← icon
   [3]  ← badge bg-danger, positioned top-right of bell
         Shows count of unread notifications (0 = badge hidden)
```

#### Component Inventory

| Component              | Bootstrap / Blazor Element                   | Notes                                                   |
|------------------------|----------------------------------------------|---------------------------------------------------------|
| Top nav bar            | `navbar navbar-dark bg-dark fixed-top`       | Height 56px; z-index above sidebar                     |
| Sidebar toggle button  | `btn btn-outline-light btn-sm` (hamburger)   | Visible on all breakpoints; hides sidebar on click      |
| Logo image             | `<img>` `navbar-brand`                       | 32×32px icon version; links to `/dashboard`             |
| App title              | `span fw-bold text-white` inside navbar-brand| "Corporate Memo System"                                 |
| Global search input    | `form-control form-control-sm`               | `placeholder="Search memos..."` width ~280px (d-none on xs) |
| Notification bell      | `btn btn-outline-light position-relative`    | Icon + `badge bg-danger` overlay for unread count       |
| User avatar dropdown   | `dropdown` + `btn btn-outline-light`         | Shows initials or placeholder avatar; dropdown menu with Profile and Logout |
| Left sidebar           | `offcanvas offcanvas-start` (xs/sm) or fixed `col-auto` (lg+) | 240px fixed on lg+; off-canvas drawer on smaller screens |
| Sidebar nav links      | `nav nav-pills flex-column`                  | Active state: `active` class on current page link       |
| Admin section          | Conditional `@if (isAdmin)` block in sidebar | Shown only to users with Admin role                     |
| Main content area      | `col` with `pt-4 px-4 pb-5` padding         | Offset by sidebar width on lg+; full-width on mobile    |
| Footer                 | `footer bg-light border-top py-2 text-muted small text-center` | Fixed at bottom |
| Error boundary         | Blazor `<ErrorBoundary>` wrapping `@Body`    | Shows user-friendly error message; logs exception       |

#### Interaction Notes

- **Sidebar collapse:** On lg+ screens, a toggle button collapses the sidebar to 0 width; main content expands to full width. State is stored in a Blazor component parameter or localStorage so it persists across page navigations within the session.
- **Off-canvas on mobile:** On xs/sm screens, the sidebar is an `offcanvas` component — hidden by default, slides in from the left when the hamburger button is pressed. A backdrop overlay appears; clicking it closes the sidebar.
- **Active nav link:** The current page's sidebar link has the `nav-link active` class, shown with `bg-primary text-white` pill styling.
- **Notification bell:** Shows a red badge with the count of unread notifications. The badge is hidden (not rendered) when count is 0. Clicking the bell opens the Notification Panel (see section 4.9) as a right-side offcanvas.
- **User dropdown:** Shows the user's display name and email as non-clickable header items, then "Profile" (links to a basic profile view, post-MVP) and "Logout" (clears authentication cookie, redirects to `/login`).
- **Global search:** Pressing Enter in the search bar navigates to the Dashboard with the search term pre-populated in the search input and results filtered. The search term is passed as a query string parameter.
- **Responsive behavior:** On md and below, the sidebar is hidden by default. The main content area spans the full width. The global search input in the navbar hides on xs (use the dashboard search bar instead).
- **Loading indicator:** A thin `progress` bar (Bootstrap `progress-bar bg-primary`) runs across the top of the main content area during Blazor component loading (NavigationManager navigation events).

---

### 4.3 Dashboard — All Documents Tab

#### Purpose

The All Documents tab shows all memos with Published status. It is the primary read-only view of organizational memos accessible to every authenticated user.

#### User Stories Served

- E3-S1: Dashboard — All Documents Tab
- E3-S3: Filtering and Sorting
- E3-S4: Simple and Advanced Search

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────┐
│ MAIN CONTENT AREA                                                       │
│                                                                         │
│  Dashboard                                                              │
│  ═══════════════════════════════════════════════════════════════        │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  All Documents (124) │  My Documents (8)  │  My Approvals (2│)      │
│  │  ─────────────       │                    │                 │       │
│  └─────────────────────────────────────────────────────────────┘       │
│     ↑ active tab (border-bottom primary)   ↑ badge counts              │
│                                                                         │
│  TOOLBAR                                                                │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  [+ New Memo]    [🔍 Search titles, content, tags...  ] [Advanced Search ▾] │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  ADVANCED SEARCH PANEL (collapsed by default, expands on click)        │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  Date From: [2026-01-01    ▾]   Date To: [2026-03-31    ▾] │       │
│  │                                                             │       │
│  │  Author:    [Select author...             ▾]               │       │
│  │                                                             │       │
│  │  Status:    [▾ All Statuses              ]                 │       │
│  │             [x] Draft  [x] Pending  [x] Approved           │       │
│  │             [x] Rejected  [x] Published                     │       │
│  │                                                             │       │
│  │  Tags:      [finance] [×]  [policy] [×]  [type tag + Enter]│       │
│  │                                                             │       │
│  │  Confidential:  (●) All  ( ) Yes  ( ) No                  │       │
│  │                                                             │       │
│  │  [Apply Filters]   [Clear All]                             │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  DATA TABLE                                                             │
│  ┌──────────────┬──────────────────────┬──────────┬────────────┬────┬──────────────┬────────┐ │
│  │ Memo Number ↕│ Title               ↕│ Author  ↕│ Date       ↕│Status│ Tags      │Actions │ │
│  ├──────────────┼──────────────────────┼──────────┼────────────┼──────┼──────────────┼────────┤ │
│  │ jsmith-      │ Q1 Budget Policy     │ John     │ 2026-03-01 │ ████ │ finance,   │ [View] │ │
│  │ 20260301-001 │ Update 2026          │ Smith    │            │Published│ q1, budget│        │ │
│  ├──────────────┼──────────────────────┼──────────┼────────────┼──────┼──────────────┼────────┤ │
│  │ ajones-      │ Remote Work Policy   │ Alice    │ 2026-02-28 │ ████ │ hr, policy,│ [View] │ │
│  │ 20260228-001 │ Amendment — March    │ Jones    │            │Published│ remote   │        │ │
│  ├──────────────┼──────────────────────┼──────────┼────────────┼──────┼──────────────┼────────┤ │
│  │ bjohnson-    │ IT Security Reminder │ Bob      │ 2026-02-25 │ ████ │ it,        │ [View] │ │
│  │ 20260225-002 │ Q1 2026              │ Johnson  │            │Published│ security │        │ │
│  ├──────────────┴──────────────────────┴──────────┴────────────┴──────┴──────────────┴────────┤ │
│  │ ...                                                                                        │ │
│  └────────────────────────────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  PAGINATION                                                             │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  Showing 1–25 of 124 memos            [«] [1] [2][3][4][5] [»]     │
│  │                                                             │       │
│  │  Rows per page: [25 ▾]                                      │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  EMPTY STATE (shown when no results match filters):                     │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │                                                             │       │
│  │            📄                                               │       │
│  │       [document icon illustration]                          │       │
│  │                                                             │       │
│  │       No memos found                                        │       │
│  │       Try adjusting your search or filters,                 │       │
│  │       or clear all filters to see all documents.            │       │
│  │                                                             │       │
│  │            [Clear Filters]                                  │       │
│  │                                                             │       │
│  └─────────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component               | Bootstrap / Blazor Element                  | Notes                                                      |
|-------------------------|---------------------------------------------|------------------------------------------------------------|
| Page title              | `h1 fs-2 fw-bold`                           | "Dashboard"                                                |
| Tab navigation          | `nav nav-tabs`                              | Three tabs; active tab underlined in primary color         |
| Tab badge               | `badge bg-secondary rounded-pill ms-1`      | Shows record count; updates when filters change            |
| Toolbar                 | `d-flex gap-2 align-items-center mb-3`      | New Memo button left; search group right                   |
| New Memo button         | `btn btn-primary`                           | Navigates to `/memos/create`                               |
| Search input            | `form-control`                              | Live search on Enter key; clear button (×) appears when populated |
| Advanced Search toggle  | `btn btn-outline-secondary btn-sm`          | Toggles collapse panel; chevron rotates 180° when open     |
| Advanced search panel   | `collapse` component                        | Contains filter controls; applies on button click          |
| Date pickers            | `<InputDate>` / `form-control`              | Bootstrap date input; From/To pair                        |
| Author select           | `<select>` / `form-select`                  | Populated from user list; "All Authors" default            |
| Status checkboxes       | `<InputCheckbox>` / `form-check`            | Multi-select; default all checked                          |
| Tag chips input         | Custom `TagFilterComponent`                 | Chip entry; × to remove; Enter to add                      |
| Confidential radio      | `<InputRadioGroup>` / `form-check`          | All / Yes / No                                             |
| Apply / Clear buttons   | `btn btn-primary` / `btn btn-outline-secondary` | Apply triggers re-query; Clear resets all filter state |
| Data table              | `table table-hover table-bordered`          | Responsive wrapper: `table-responsive`                     |
| Column headers          | `<th>` with sort icons                      | Up/down arrows: `↑↓` neutral, `↑` asc active, `↓` desc active; pointer cursor |
| Status badge            | `badge` per color palette mapping           | Colored pill per status value                              |
| View button             | `btn btn-sm btn-outline-primary`            | Navigates to `/memos/{id}`                                 |
| Pagination              | `pagination` component                      | Shows page numbers; ellipsis for large page counts         |
| Rows per page           | `<select>` `form-select form-select-sm`     | Options: 10, 25, 50, 100                                   |
| Empty state             | Centered `div` with SVG icon + text        | Shown when query returns 0 results                         |

#### Interaction Notes

- **Tab switching:** Clicking a tab switches the data source; filter and sort state resets to defaults unless the tab has previously been visited in the same session (per requirement 3.3.3: filter state persists for the session).
- **Column sort:** Clicking a column header toggles sort direction. The clicked column shows the active sort arrow in primary color; all other columns show neutral `↕` arrows.
- **Search:** Pressing Enter or clicking a search icon button submits the search query. The URL is updated with `?q=querytext` so the browser back button works correctly.
- **Advanced Search:** The panel animates open/closed using Bootstrap `collapse`. Filters apply when the user clicks "Apply Filters," not on each individual change, to prevent excessive queries while adjusting multiple filters.
- **Loading state:** When a query is in progress (after search or filter change), the table body shows a skeleton loading row (three animated gray bars) and the toolbar shows a spinner.
- **Status badge in table:** Uses `badge` with color per palette. Text is included in the badge (not icon-only) per accessibility principle 5.
- **Actions column:** Only "View" appears in All Documents; context-sensitive actions appear in My Documents and My Approvals tabs.
- **Responsive behavior:** On md and below, the table switches to a card-list view where each memo is a `card` with the key fields. The Memo Number and Actions appear at the top of each card. Alternatively, `table-responsive` with horizontal scroll is acceptable for MVP.
- **"+ New Memo" button:** Always visible in the toolbar regardless of which tab is active.

---

### 4.4 Dashboard — My Documents Tab

#### Purpose

The My Documents tab shows all memos created by the currently authenticated user, across all statuses. It allows the author to manage their memos — editing drafts, resubmitting rejected memos, and tracking published memos.

#### User Stories Served

- E3-S2: Dashboard — My Documents Tab
- E1-S4: Memo Creation and Edit UI (entry point for editing)

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Same toolbar and advanced search panel as All Documents tab]          │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  All Documents (124) │  My Documents (8)  │  My Approvals (2│)      │
│  │                      │  ───────────────   │                 │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  DATA TABLE — My Documents                                              │
│  ┌──────────────┬────────────────────────┬────────┬────────────┬───────────┬──────────┬──────────────────────┐ │
│  │ Memo Number ↕│ Title                 ↕│Author ↕│ Date       ↕│  Status   │ Tags     │ Actions              │ │
│  ├──────────────┼────────────────────────┼────────┼────────────┼───────────┼──────────┼──────────────────────┤ │
│  │ jsmith-      │ Draft Internal Memo    │ John   │ 2026-03-02 │ ┌───────┐ │ internal │ [View] [Edit][Delete] │ │
│  │ 20260302-002 │ on Procurement Policy  │ Smith  │            │ │ Draft │ │          │                      │ │
│  │              │                        │        │            │ └───────┘ │          │                      │ │
│  ├──────────────┼────────────────────────┼────────┼────────────┼───────────┼──────────┼──────────────────────┤ │
│  │ jsmith-      │ Q4 Expense Report      │ John   │ 2026-03-01 │ ┌──────────────────┐ │ finance  │ [View]  │ │
│  │ 20260301-001 │ Guidelines Update      │ Smith  │            │ │ Pending Approval │ │ q4       │         │ │
│  │              │                        │        │            │ └──────────────────┘ │          │         │ │
│  ├──────────────┼────────────────────────┼────────┼────────────┼───────────┼──────────┼──────────────────────┤ │
│  │ jsmith-      │ Staff Travel Policy    │ John   │ 2026-02-20 │ ┌──────────┐         │ travel,  │ [View] [Edit]│
│  │ 20260220-001 │ v2.1 (Rejected)        │ Smith  │            │ │ Rejected │         │ hr       │         │ │
│  │              │                        │        │            │ └──────────┘         │          │         │ │
│  ├──────────────┼────────────────────────┼────────┼────────────┼───────────┼──────────┼──────────────────────┤ │
│  │ jsmith-      │ Q1 Budget Policy       │ John   │ 2026-03-01 │ ┌───────────┐        │ finance, │ [View]  │ │
│  │ 20260301-002 │ Update 2026            │ Smith  │            │ │ Published │        │ q1       │         │ │
│  │              │                        │        │            │ └───────────┘        │          │         │ │
│  └──────────────┴────────────────────────┴────────┴────────────┴───────────┴──────────┴──────────────────────┘ │
│                                                                         │
│  ACTIONS LEGEND:                                                        │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  Draft status:    [View] [Edit] [Delete]                    │       │
│  │  Pending status:  [View]                                    │       │
│  │  Rejected status: [View] [Edit]                             │       │
│  │  Approved status: [View]                                    │       │
│  │  Published status:[View]                                    │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  DELETE CONFIRMATION TOAST / MODAL:                                     │
│  ┌─────────────────────────────────────┐                               │
│  │  Delete this memo?                  │                               │
│  │                                     │                               │
│  │  "Draft Internal Memo on..."        │                               │
│  │  This action cannot be undone.      │                               │
│  │                                     │                               │
│  │  [Cancel]   [Delete]                │ ← Delete is btn-danger        │
│  └─────────────────────────────────────┘                               │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component              | Bootstrap / Blazor Element                   | Notes                                                       |
|------------------------|----------------------------------------------|-------------------------------------------------------------|
| Tab (active)           | `nav-link active` (My Documents tab)         | Same nav-tabs component as All Documents                    |
| Status badge           | `badge` per color palette                    | Draft=secondary, Pending=warning, Approved=success, Rejected=danger, Published=info |
| View button            | `btn btn-sm btn-outline-primary`             | Always present                                              |
| Edit button            | `btn btn-sm btn-outline-secondary`           | Shown for Draft and Rejected statuses only                  |
| Delete button          | `btn btn-sm btn-outline-danger`              | Shown for Draft status only                                 |
| Delete confirm modal   | `modal modal-dialog-centered`                | Small modal; Cancel and Delete (btn-danger) buttons         |

#### Interaction Notes

- **Edit button:** Navigates to `/memos/{id}/edit` (Memo Create/Edit Page pre-populated with existing data).
- **Delete button:** Opens an inline confirm modal. On confirm, a DELETE API call is made. On success, the row is removed with a smooth CSS fade-out and a success toast appears: "Memo deleted successfully."
- **Pending Approval row:** The Edit and Delete actions are not available — the memo cannot be modified while awaiting approval. A tooltip on hover of the row explains: "Memo is pending approval and cannot be edited."
- **Rejected row:** Edit is available so the author can revise and resubmit. The Edit button navigates to the edit page; within the edit page, the [Submit for Approval] button allows resubmission.
- **Published row:** Only View is available. No modification is allowed post-publication.
- **Loading state on delete:** The Delete button shows a spinner and is disabled while the delete request is pending.
- **Empty state:** "You have not created any memos yet. Click '+ New Memo' to get started."

---

### 4.5 Dashboard — My Approvals Tab

#### Purpose

The My Approvals tab shows all memos where the current user is an assigned approver and the memo is currently in Pending Approval status. It provides quick approve/reject actions directly from the table row.

#### User Stories Served

- E2-S3: Approval Decision UI — My Approvals Tab
- E3-S3: Dashboard — My Approvals Tab

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Same toolbar — but no "+ New Memo" in advanced search filter area]    │
│  [No status filter in advanced search (all shown here are Pending)]     │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │  All Documents (124) │  My Documents (8)  │  My Approvals (2│)      │
│  │                      │                    │  ────────────── │       │
│  └─────────────────────────────────────────────────────────────┘       │
│                                                                         │
│  DATA TABLE — My Approvals                                              │
│  ┌──────────────┬──────────────────────┬──────────┬────────────┬──────────────────┬──────────┬────────────────────────┐
│  │ Memo Number ↕│ Title               ↕│ Author  ↕│ Date       ↕│     Status       │ Tags     │ Actions                │
│  ├──────────────┼──────────────────────┼──────────┼────────────┼──────────────────┼──────────┼────────────────────────┤
│  │ jsmith-      │ Q4 Expense Report    │ John     │ 2026-03-01 │ ┌──────────────┐  │ finance, │ [View][Approve][Reject] │
│  │ 20260301-001 │ Guidelines Update    │ Smith    │            │ │Pending Apprvl│  │ q4       │                        │
│  │              │                      │          │            │ └──────────────┘  │          │                        │
│  ├──────────────┼──────────────────────┼──────────┼────────────┼──────────────────┼──────────┼────────────────────────┤
│  │ ajones-      │ New Stationery       │ Alice    │ 2026-03-02 │ ┌──────────────┐  │ admin,   │ [View][Approve][Reject] │
│  │ 20260302-001 │ Procurement Request  │ Jones    │            │ │Pending Apprvl│  │ supplies │                        │
│  │              │                      │          │            │ └──────────────┘  │          │                        │
│  └──────────────┴──────────────────────┴──────────┴────────────┴──────────────────┴──────────┴────────────────────────┘
│                                                                         │
│  ACTIONS NOTES:                                                         │
│  [View]    → Opens Memo Detail page (read-only)                         │
│  [Approve] → Opens Approve/Reject Modal pre-set to "Approve"           │
│  [Reject]  → Opens Approve/Reject Modal pre-set to "Reject"            │
│                                                                         │
│  EMPTY STATE (when user has no pending approvals):                      │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │                      ✓                                      │       │
│  │         [checkmark illustration]                            │       │
│  │                                                             │       │
│  │         You're all caught up!                               │       │
│  │         No memos are waiting for your approval.             │       │
│  └─────────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component              | Bootstrap / Blazor Element                   | Notes                                                       |
|------------------------|----------------------------------------------|-------------------------------------------------------------|
| Tab badge              | `badge bg-danger rounded-pill ms-1`          | Uses danger (red) color to indicate action required         |
| Status badge           | `badge bg-warning text-dark`                 | All rows are Pending Approval; badge is consistent          |
| View button            | `btn btn-sm btn-outline-primary`             | Opens Memo Detail in read-only mode                        |
| Approve button         | `btn btn-sm btn-success`                     | Filled success green; opens Approve modal                  |
| Reject button          | `btn btn-sm btn-danger`                      | Filled danger red; opens Reject modal                      |
| Approve/Reject modal   | See section 4.8                              | Triggered inline; modal overlay                            |
| Empty state            | Centered `div` with success icon            | "You're all caught up!" message                            |

#### Interaction Notes

- **Approve button click:** Opens the Approve/Reject Modal (section 4.8) with the decision pre-set to "Approve". Modal title shows "Approve Memo".
- **Reject button click:** Opens the Approve/Reject Modal with decision pre-set to "Reject". Modal title shows "Reject Memo". Comment is required.
- **After decision:** On successful modal confirmation, the table row fades out and a success toast appears: "Memo approved." or "Memo rejected." The tab badge count decrements.
- **Tab badge color:** The My Approvals badge uses `bg-danger` (red) instead of `bg-secondary` when count > 0, drawing attention to pending actions.
- **View button:** Navigates to Memo Detail page. Approve/Reject action buttons also appear in the Memo Detail page action bar for this user's context, so actions can be taken from either location.
- **No status filter in advanced search:** All memos in My Approvals are by definition Pending Approval, so the status filter is hidden for this tab to avoid confusion.
- **Empty state icon:** A large checkmark (Bootstrap Icons `bi-check-circle` or equivalent SVG) in success green conveys a positive "inbox zero" state.

---

### 4.6 Memo Create / Edit Page

#### Purpose

The Memo Create/Edit page allows authenticated users to compose a new memo or edit an existing Draft or Rejected memo. It provides all required metadata fields, a rich text editor, file attachments, tag entry, recipient selection, and approver assignment.

#### User Stories Served

- E1-S4: Memo Creation and Edit UI
- E1-S2: Create Memo Command
- E1-S3: Update and Submit Memo Commands
- E1-S5: File Attachment Upload UI
- E1-S6: Tag Input Component
- E1-S7: User Autocomplete Component

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ MEMO CREATE / EDIT PAGE  (Blazor page: /memos/create  or  /memos/{id}/edit)             │
│                                                                                         │
│  ← Back to Dashboard       Create New Memo                                              │
│  ══════════════════════════════════════════════════════════════════════════             │
│                                                                                         │
│  ┌─────────────────────────────────────────────┐  ┌──────────────────────────────────┐ │
│  │  LEFT COLUMN  (col-lg-8)                    │  │  RIGHT COLUMN (col-lg-4, sticky) │ │
│  │                                             │  │                                  │ │
│  │  TITLE                                      │  │  MEMO DETAILS                    │ │
│  │  ┌─────────────────────────────────────┐    │  │  ┌──────────────────────────┐    │ │
│  │  │ Enter memo title here...            │    │  │  │ Memo Number              │    │ │
│  │  └─────────────────────────────────────┘    │  │  │ (Auto-assigned on save)  │    │ │
│  │                              72 / 100 chars │  │  └──────────────────────────┘    │ │
│  │                                             │  │                                  │ │
│  │  CONTENT                                    │  │  ┌──────────────────────────┐    │ │
│  │  ┌─────────────────────────────────────┐    │  │  │ Author                   │    │ │
│  │  │ [B][I][U] [•≡][1≡] [🔗][—] [Source]│    │  │  │ John Smith (read-only)   │    │ │
│  │  │─────────────────────────────────────│    │  │  └──────────────────────────┘    │ │
│  │  │                                     │    │  │                                  │ │
│  │  │  Type memo content here...          │    │  │  ┌──────────────────────────┐    │ │
│  │  │                                     │    │  │  │ Date Created             │    │ │
│  │  │                                     │    │  │  │ 2026-03-02 (read-only)   │    │ │
│  │  │                                     │    │  │  └──────────────────────────┘    │ │
│  │  │                                     │    │  │                                  │ │
│  │  │                                     │    │  │  ┌──────────────────────────┐    │ │
│  │  │                                     │    │  │  │ Status                   │    │ │
│  │  │                                     │    │  │  │ ┌──────┐                 │    │ │
│  │  └─────────────────────────────────────┘    │  │  │ │Draft │                 │    │ │
│  │       Plain text equivalent: 347 / 1000     │  │  │ └──────┘ badge-secondary │    │ │
│  │                                             │  │  └──────────────────────────┘    │ │
│  │  ATTACHMENTS                                │  │                                  │ │
│  │  ┌─────────────────────────────────────┐    │  │  HASH TAGS                       │ │
│  │  │  ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐  │    │  │  ┌──────────────────────────┐    │ │
│  │  │  │  📎 Drag files here or         │  │    │  │  │[finance][×] [q1][×]      │    │ │
│  │  │  │  [Browse Files]               │  │    │  │  │[type tag, press Enter...] │    │ │
│  │  │  │  PDF, DOCX, XLSX, PNG, JPG    │  │    │  │  └──────────────────────────┘    │ │
│  │  │  │  Max 10MB per file            │  │    │  │                                  │ │
│  │  │  └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘  │    │  │  DISTRIBUTION LIST               │ │
│  │  │                                     │    │  │  ┌──────────────────────────┐    │ │
│  │  │  UPLOADED FILES                     │    │  │  │ To (required)            │    │ │
│  │  │  ┌────────────────────────────────┐ │    │  │  │[alice@corp.com][×]        │    │ │
│  │  │  │ 📄 Q1-Budget.pdf  (245 KB)  [×]│ │    │  │  │[type name or email...]   │    │ │
│  │  │  │ 📄 Policy-v2.docx (88 KB)   [×]│ │    │  │  └──────────────────────────┘    │ │
│  │  │  └────────────────────────────────┘ │    │  │                                  │ │
│  │  │  Total: 333 KB of 50 MB used        │    │  │  ┌──────────────────────────┐    │ │
│  │  └─────────────────────────────────────┘    │  │  │ CC (optional)            │    │ │
│  │                                             │  │  │[type name or email...]   │    │ │
│  │                                             │  │  └──────────────────────────┘    │ │
│  │                                             │  │                                  │ │
│  │                                             │  │  APPROVERS                       │ │
│  │                                             │  │  ┌──────────────────────────┐    │ │
│  │                                             │  │  │ Approvers (optional)     │    │ │
│  │                                             │  │  │ ⠿ 1. Bob Johnson [×]     │    │ │
│  │                                             │  │  │ ⠿ 2. Carol White [×]     │    │ │
│  │                                             │  │  │[type name or email...]   │    │ │
│  │                                             │  │  │                          │    │ │
│  │                                             │  │  │ ℹ No approvers = auto-   │    │ │
│  │                                             │  │  │   publish on submit      │    │ │
│  │                                             │  │  └──────────────────────────┘    │ │
│  │                                             │  │                                  │ │
│  │                                             │  │  CONFIDENTIAL                    │ │
│  │                                             │  │  ┌──────────────────────────┐    │ │
│  │                                             │  │  │ Confidential Memo        │    │ │
│  │                                             │  │  │ [ Toggle OFF ○──●── ]    │    │ │
│  │                                             │  │  │ Mark as confidential     │    │ │
│  │                                             │  │  └──────────────────────────┘    │ │
│  │                                             │  │                                  │ │
│  │                                             │  │  ACTION BUTTONS                  │ │
│  │                                             │  │  ┌──────────────────────────┐    │ │
│  │                                             │  │  │ [Save Draft]             │    │ │
│  │                                             │  │  │ btn-outline-secondary    │    │ │
│  │                                             │  │  │                          │    │ │
│  │                                             │  │  │ [Submit for Approval]    │    │ │
│  │                                             │  │  │ btn-primary              │    │ │
│  │                                             │  │  │                          │    │ │
│  │                                             │  │  │ [Cancel]                 │    │ │
│  │                                             │  │  │ btn-link text-danger     │    │ │
│  │                                             │  │  └──────────────────────────┘    │ │
│  └─────────────────────────────────────────────┘  └──────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────────────────┘

FILE UPLOAD PROGRESS (shown while uploading):
┌──────────────────────────────────────────────┐
│ 📄 Annual-Report.pdf                         │
│ ████████████████░░░░░░░░░  65%               │
│ Uploading... 6.5 MB of 10 MB                 │
└──────────────────────────────────────────────┘

AUTO-PUBLISH CONFIRMATION (shown when Submit clicked with no approvers):
┌────────────────────────────────────────────┐
│  No approvers assigned                     │
│                                            │
│  This memo will be published immediately   │
│  to all recipients in the distribution     │
│  list. Continue?                           │
│                                            │
│  [Cancel]    [Publish Now]                 │
└────────────────────────────────────────────┘
```

#### Component Inventory

| Component                  | Bootstrap / Blazor Element                      | Notes                                                          |
|----------------------------|-------------------------------------------------|----------------------------------------------------------------|
| Page layout                | `row g-4` with `col-lg-8` and `col-lg-4`        | Two-column on lg+; stacks to single column on md and below     |
| Back link                  | `btn btn-sm btn-link`                           | `← Back to Dashboard`; navigates to previous location          |
| Page heading               | `h1 fs-3 fw-bold`                               | "Create New Memo" or "Edit Memo"                               |
| Title input                | `<InputText>` / `form-control`                  | `maxlength="100"`; character counter below right               |
| Character counter          | `small text-muted text-end`                     | Updates on each keystroke; turns `text-danger` at 90+          |
| Rich text editor           | `RichTextEditorComponent` (Quill.js via JSInterop) | Toolbar: Bold, Italic, Underline, Bullet list, Numbered list  |
| Content counter            | `small text-muted`                              | Plain-text-equivalent counter; turns `text-danger` at 900+     |
| Drop zone                  | `div` with drag-and-drop events                 | Dashed border; highlights on drag-over; `btn btn-outline-secondary` for browse |
| File list                  | `ul list-unstyled`                              | Each file: icon + name + size + delete button                  |
| File delete button         | `btn btn-sm btn-outline-danger`                 | Sends DELETE /api/memos/{id}/attachments/{fileId}             |
| Upload progress            | `progress progress-bar bg-primary`              | Per-file progress bar; shown while upload is in progress       |
| Total size indicator       | `small text-muted`                              | "X KB of 50 MB used"; turns `text-danger` if limit approached  |
| Memo Number (read-only)    | `form-control-plaintext font-monospace`         | "Auto-assigned on save" until first save                       |
| Author (read-only)         | `form-control-plaintext`                        | Display name from session                                      |
| Date Created (read-only)   | `form-control-plaintext`                        | UTC date formatted as `YYYY-MM-DD`                             |
| Status badge               | `badge` per color palette                       | Updates reactively as status changes                           |
| Tag input                  | `TagInputComponent`                             | Chip entry; comma or Enter to add; × to remove each tag        |
| To/CC autocomplete         | `UserAutocompleteComponent`                     | Types into field, shows dropdown of users; adds as chip        |
| Approvers list             | `UserAutocompleteComponent` + drag handles      | Ordered list; drag handle icon (⠿) for reordering (post-MVP); × to remove |
| Approver hint text         | `small text-muted`                              | "No approvers = auto-publish on submit"                        |
| Confidential toggle        | Bootstrap 5 `form-switch`                       | `<InputCheckbox>` rendered as toggle switch                    |
| Save Draft button          | `btn btn-outline-secondary w-100`               | Saves without validation of recipients/approvers               |
| Submit for Approval button | `btn btn-primary w-100`                         | Validates To recipients and title before submit                |
| Cancel button              | `btn btn-link text-danger w-100`                | Prompts confirm if unsaved changes; navigates to Dashboard      |
| Auto-publish confirm modal | `modal modal-dialog-centered modal-sm`          | Shown when Submit clicked with 0 approvers                     |
| Validation messages        | `ValidationMessage` / `invalid-feedback`        | Per-field inline messages below each input                     |

#### Interaction Notes

- **Two-column layout:** The right column uses `position-sticky top: 70px` so it stays visible as the user scrolls down the left column's content area. On mobile (below lg), the right column appears below the left column in the natural stacking order.
- **Title character counter:** Counter increments on every keystroke. At 90/100, turns `text-warning`. At 100/100, input stops accepting characters and counter turns `text-danger`.
- **Rich text editor:** Implemented as a Blazor component wrapping Quill.js via JavaScript Interop. The toolbar contains: Bold, Italic, Underline, Bullet List, Numbered List. The component reads back plain-text equivalent for the 1000-character content limit check.
- **File drag-and-drop:** Dragging a file over the drop zone adds a visual highlight (dashed border color changes to primary blue). Dropping triggers upload immediately. Multiple files can be dropped at once. Files exceeding 10 MB are rejected immediately with an inline error: "File exceeds the maximum size of 10 MB."
- **Tag input:** Pressing Enter or comma after typing a tag word adds it as a chip. Pressing Backspace on an empty input removes the last chip. Tags are trimmed of whitespace and deduplicated client-side.
- **Autocomplete dropdowns:** Typing 2+ characters in the To, CC, or Approvers fields triggers a debounced search against `/api/users/suggest?q=...`. Results appear as a `dropdown-menu`. Clicking or pressing Enter on a result adds the user as a chip. The same user cannot be added twice to the same field.
- **Save Draft:** Triggers `CreateMemoCommand` (on first save) or `UpdateMemoCommand` (on subsequent saves). On success, the Memo Number field populates with the generated number, and a success toast appears: "Draft saved."
- **Submit for Approval:** Validates that Title is non-empty and To recipients list has at least one entry. If approvers list is empty, the auto-publish confirmation modal appears. On confirm, triggers `SubmitMemoCommand`. On success, navigates to Memo Detail page.
- **Cancel:** If there are unsaved changes (tracked by a dirty flag on the Blazor component), a browser `beforeunload` confirmation is shown. If no unsaved changes, navigates directly to Dashboard.
- **Edit mode:** When navigating to `/memos/{id}/edit`, the page pre-populates all fields from the existing memo. The heading changes to "Edit Memo". Memo Number, Author, and Date Created are read-only and show the original values.

---

### 4.7 Memo Detail / View Page

#### Purpose

The Memo Detail page displays the full read-only content of a memo, including its metadata, rich text content, attachments, distribution recipients, and approval history. Context-sensitive action buttons appear based on the current user's role and the memo's status.

#### User Stories Served

- E1-S8: Memo Detail View
- E2-S4: Approval History Display
- E2-S5: Approve/Reject Actions from Detail Page

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│ MEMO DETAIL PAGE  (/memos/{id})                                                 │
│                                                                                 │
│  ← Back to Dashboard                                                            │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  MEMO HEADER                                                            │    │
│  │                                                                         │    │
│  │  jsmith-20260301-001                      ┌───────────┐ ┌────────────┐ │    │
│  │  font-monospace text-muted small          │ Published │ │Confidential│ │    │
│  │                                           │ bg-info   │ │ bg-warning │ │    │
│  │  Q1 Budget Policy Update 2026             └───────────┘ └────────────┘ │    │
│  │  h2 fw-bold                                                             │    │
│  │                                                                         │    │
│  │  ─────────────────────────────────────────────────────────────────────  │    │
│  │                                                                         │    │
│  │  META STRIP                                                             │    │
│  │  Author: John Smith  |  Created: 2026-03-01  |  Approved: 2026-03-02   │    │
│  │  Tags: [finance] [q1] [budget]                                          │    │
│  │                                                                         │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ACTION BAR (context-sensitive — shown at top right of header area)             │
│  ┌─────────────────────────────────────────────────────────────────────────┐    │
│  │  IF Draft + owner:         [Edit]  [Submit for Approval]  [Delete]      │    │
│  │  IF Pending + owner:       [Edit]  [Recall]                             │    │
│  │  IF Pending + approver:    [Approve]  [Reject]                          │    │
│  │  IF Published:             [Export PDF]  [Print]                        │    │
│  └─────────────────────────────────────────────────────────────────────────┘    │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐               │
│  │  MEMO CONTENT                                               │               │
│  │  (sanitised rich text rendered as HTML)                     │               │
│  │                                                             │               │
│  │  This memorandum confirms the updated budget policy         │               │
│  │  for Q1 2026. All departments are required to submit        │               │
│  │  expense reports using the revised Form B-12 by no          │               │
│  │  later than 15 March 2026.                                  │               │
│  │                                                             │               │
│  │  Key changes include:                                       │               │
│  │   • Maximum per-diem rate increased to $85/day             │               │
│  │   • Travel pre-approval threshold lowered to $500          │               │
│  │   • All receipts must be submitted digitally               │               │
│  │                                                             │               │
│  │  Please review the attached policy document for full        │               │
│  │  details.                                                   │               │
│  └─────────────────────────────────────────────────────────────┘               │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐               │
│  │  ATTACHMENTS (2 files)                                      │               │
│  │  ────────────────────────────────────────────────────────── │               │
│  │  📄  Q1-Budget-Policy-v3.pdf          245 KB  [Download]    │               │
│  │  📊  Budget-Summary-Q1-2026.xlsx       88 KB  [Download]    │               │
│  └─────────────────────────────────────────────────────────────┘               │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐               │
│  │  DISTRIBUTION                                               │               │
│  │  ────────────────────────────────────────────────────────── │               │
│  │  To:   Alice Jones, Bob Johnson, Carol White                │               │
│  │  CC:   Finance Team DL, HR Department DL                    │               │
│  └─────────────────────────────────────────────────────────────┘               │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────┐               │
│  │  APPROVAL HISTORY                                           │               │
│  │  ────────────────────────────────────────────────────────── │               │
│  │                                                             │               │
│  │  ●─────────────────────────────────────────────────────     │               │
│  │  │                                                          │               │
│  │  │  ┌──┐  Bob Johnson                                       │               │
│  │  │  │BJ│  bob.johnson@corp.com                              │               │
│  │  │  └──┘  ✓ Approved   2026-03-02 09:14 UTC                 │               │
│  │  │        "Looks good, approved."                           │               │
│  │  │                                                          │               │
│  │  ●─────────────────────────────────────────────────────     │               │
│  │  │                                                          │               │
│  │  │  ┌──┐  Carol White                                       │               │
│  │  │  │CW│  carol.white@corp.com                              │               │
│  │  │  └──┘  ✓ Approved   2026-03-02 10:02 UTC                 │               │
│  │  │        (no comment)                                      │               │
│  │  │                                                          │               │
│  │  ○  (pending approver example — if still pending)           │               │
│  │  │  ┌──┐  David Brown                                       │               │
│  │  │  │DB│  d.brown@corp.com                                  │               │
│  │  │  └──┘  ⏳ Pending                                        │               │
│  │  │                                                          │               │
│  │  ●─────────────────────────────────────────────────────     │               │
│  │     ┌──┐  Eve Adams (example — rejected)                    │               │
│  │     │EA│  e.adams@corp.com                                  │               │
│  │     └──┘  ✗ Rejected   2026-03-02 11:30 UTC                 │               │
│  │           "Please revise section 3 before resubmitting."    │               │
│  │                                                             │               │
│  └─────────────────────────────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component                    | Bootstrap / Blazor Element                     | Notes                                                         |
|------------------------------|------------------------------------------------|---------------------------------------------------------------|
| Back link                    | `btn btn-sm btn-link`                          | `← Back to Dashboard`                                        |
| Memo number                  | `span font-monospace text-muted small`         | Monospace per typography spec                                 |
| Memo title                   | `h2 fw-bold mb-2`                              | Full title; wraps to multiple lines if long                   |
| Status badge                 | `badge` per color palette                      | Color-coded; adjacent to title                                |
| Confidential badge           | `badge bg-warning text-dark`                   | Shown only when `IsConfidential == true`                      |
| Meta strip                   | `d-flex flex-wrap gap-3 text-muted small`      | Author, created date, approved date separated by `|`          |
| Tag chips (read-only)        | `badge bg-light text-dark border me-1`         | Read-only display; not interactive on detail page             |
| Action bar                   | `d-flex gap-2 ms-auto`                         | Right-aligned; buttons differ per context (see below)         |
| Edit button                  | `btn btn-outline-secondary btn-sm`             | Navigates to edit page                                        |
| Submit for Approval button   | `btn btn-primary btn-sm`                       | Triggers submit flow                                          |
| Delete button                | `btn btn-outline-danger btn-sm`                | Opens confirm modal                                           |
| Recall button                | `btn btn-warning btn-sm`                       | Recalls memo from Pending back to Draft (post-MVP consideration; noted here for design completeness) |
| Approve button               | `btn btn-success btn-sm`                       | Opens Approve/Reject modal; shown to approvers only           |
| Reject button                | `btn btn-danger btn-sm`                        | Opens Approve/Reject modal; shown to approvers only           |
| Export PDF button            | `btn btn-outline-secondary btn-sm`             | Shown for Published memos; triggers PDF generation (post-MVP) |
| Print button                 | `btn btn-outline-secondary btn-sm`             | Triggers `window.print()` via JSInterop; post-MVP             |
| Content area                 | `div prose-content border rounded p-4`         | Renders sanitised HTML from rich text editor; max-width 80ch  |
| Attachments section          | `card card-body`                               | File icon + name + size + download link per file              |
| Download link                | `a` with `href` to `/api/attachments/{id}`     | `download` attribute triggers browser download dialog         |
| Distribution section         | `card card-body`                               | Two rows: To recipients, CC recipients                        |
| Approval history section     | `card card-body`                               | Vertical timeline component                                   |
| Timeline step (approved)     | Circle icon (●) in success green + info row    | Avatar initials in `rounded-circle bg-secondary text-white`   |
| Timeline step (rejected)     | Circle icon (●) in danger red + info row       | Rejection comment shown in `blockquote` or `text-muted`       |
| Timeline step (pending)      | Open circle icon (○) in secondary gray         | No date shown; "Pending" label in warning yellow              |
| Timeline connector line      | `border-start border-2 ms-3`                   | Vertical line connecting step circles                         |

#### Interaction Notes

- **Action bar context rules:**
  - Draft + current user is author: [Edit] [Submit for Approval] [Delete]
  - Pending Approval + current user is author: [Edit] [Recall] (Recall transitions memo back to Draft; included in design for completeness per requirements section 3.1.3)
  - Pending Approval + current user is an assigned approver: [Approve] [Reject]
  - Approved or Published: [Export PDF] [Print] (both are post-MVP features; buttons shown but disabled with tooltip "Coming soon" in MVP)
  - If none of the above apply (general user viewing a published memo): no action bar shown
- **Approval timeline:** Steps are rendered in the order approvers were assigned. Each step shows: avatar (initials in a colored circle), display name, email, decision badge, timestamp, and optional comment. If the approval is still pending, no timestamp is shown.
- **Avatar initials:** Generated from the approver's display name (first letter of first name + first letter of last name). Background color is deterministically derived from the user's name using a simple hash to a palette of Bootstrap secondary colors.
- **Attachment icons:** PDF files show a red document icon; DOCX shows blue; XLSX shows green; images show a thumbnail if available, otherwise a generic image icon.
- **Loading state:** The page shows a `placeholder` skeleton (Bootstrap 5 placeholders) while memo data is being fetched. Three gray bars of varying widths simulate the header, content, and sections loading.
- **Error state:** If the memo ID does not exist, a full-page `alert alert-danger` is shown: "Memo not found. It may have been deleted or you may not have permission to view it."
- **Confidential viewing (MVP):** The confidential badge is displayed, but content access control enforcement is deferred to post-MVP per requirements section 4.1.
- **Responsive behavior:** On mobile, the action bar buttons stack vertically below the title. The two-column approval history remains single column. Attachment download links are full-width touch targets.

---

### 4.8 Approve / Reject Modal

#### Purpose

The Approve/Reject modal is a focused dialog that allows an approver to record their decision (Approve or Reject) on a specific memo, with an optional comment for approval and a required comment for rejection.

#### User Stories Served

- E2-S3: Approval Decision Handler
- E2-S2: Rejection Comment Storage

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PAGE DIMMED BY MODAL BACKDROP (opacity: 0.5)                           │
│                                                                         │
│         ┌────────────────────────────────────────────────────┐         │
│         │  MODAL HEADER                                       │         │
│         │  ────────────────────────────────────────────────── │         │
│         │  ✓ Approve Memo                              [×]    │         │
│         │  (or)                                               │         │
│         │  ✗ Reject Memo                               [×]    │         │
│         │                                                     │         │
│         │  MODAL BODY                                         │         │
│         │  ────────────────────────────────────────────────── │         │
│         │                                                     │         │
│         │  Memo Reference                                     │         │
│         │  ┌───────────────────────────────────────────┐      │         │
│         │  │ jsmith-20260301-001                       │      │         │
│         │  │ Q1 Budget Policy Update 2026              │      │         │
│         │  └───────────────────────────────────────────┘      │         │
│         │  (read-only, bg-light)                              │         │
│         │                                                     │         │
│         │  Comment                                            │         │
│         │  (optional for Approve / required for Reject)       │         │
│         │  ┌───────────────────────────────────────────┐      │         │
│         │  │                                           │      │         │
│         │  │  Type your comment here...                │      │         │
│         │  │                                           │      │         │
│         │  │                                           │      │         │
│         │  └───────────────────────────────────────────┘      │         │
│         │  textarea rows="4", max 500 chars                   │         │
│         │                                                     │         │
│         │  (Reject mode only):                                │         │
│         │  ⚠  A comment is required when rejecting a memo.    │         │
│         │  (shown as invalid-feedback if submitted empty)     │         │
│         │                                                     │         │
│         │  MODAL FOOTER                                       │         │
│         │  ────────────────────────────────────────────────── │         │
│         │                                                     │         │
│         │  [Cancel]          [Confirm Approve]                │         │
│         │  btn-outline-       btn-success                     │         │
│         │  secondary                                          │         │
│         │  (or)                                               │         │
│         │  [Cancel]          [Confirm Reject]                 │         │
│         │  btn-outline-       btn-danger                      │         │
│         │  secondary                                          │         │
│         │                                                     │         │
│         │  (loading state — button shows spinner):            │         │
│         │  [Cancel]          [⟳ Confirming...]               │         │
│         │                    (disabled)                       │         │
│         │                                                     │         │
│         └────────────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component              | Bootstrap / Blazor Element                     | Notes                                                           |
|------------------------|------------------------------------------------|-----------------------------------------------------------------|
| Modal container        | `modal fade modal-dialog-centered`             | Centered vertically and horizontally; max-width 500px           |
| Modal header           | `modal-header`                                 | Title varies by decision type; close button `×` top right       |
| Modal title            | `modal-title h5`                               | "Approve Memo" (with success icon) or "Reject Memo" (with danger icon) |
| Close button           | `btn-close`                                    | Dismisses modal without action; same as Cancel                  |
| Memo reference         | `form-control-plaintext bg-light rounded p-2`  | Read-only; shows memo number (monospace) and title              |
| Comment textarea       | `<InputTextArea>` / `form-control`             | `rows="4"`, `maxlength="500"`, `placeholder="Type your comment here..."` |
| Required hint          | `small text-muted`                             | "Optional" for Approve; "Required" for Reject                   |
| Validation message     | `invalid-feedback`                             | Shown below textarea only in Reject mode if submitted empty     |
| Cancel button          | `btn btn-outline-secondary`                    | Dismisses modal; no state change                                |
| Confirm Approve button | `btn btn-success`                              | Triggers `ApproveMemoCommand`                                   |
| Confirm Reject button  | `btn btn-danger`                               | Triggers `RejectMemoCommand`; requires non-empty comment        |

#### Interaction Notes

- **Triggered from:** (1) [Approve] or [Reject] buttons in My Approvals table row; (2) [Approve] or [Reject] buttons in Memo Detail action bar.
- **Decision pre-selection:** The modal opens pre-configured for either Approve or Reject based on which button was clicked. The user cannot switch decision type within the modal.
- **Comment requirement:** For Reject, the comment textarea shows `required` styling and the Confirm button is disabled until at least 1 character is entered. For Approve, the comment is optional.
- **Loading state:** After clicking Confirm, both buttons are disabled and the Confirm button displays a spinner: "⟳ Confirming...". The modal remains open during the API call.
- **Success:** On successful API response, the modal closes with a fade-out animation. A success toast appears at the top-right: "Memo approved." (green) or "Memo rejected." (red). If triggered from My Approvals table, the row fades out and the badge count decrements. If triggered from Memo Detail, the status badge and action bar update reactively.
- **Error:** If the API call fails (network error, server error), the modal stays open and an `alert alert-danger` appears inside the modal body: "An error occurred. Please try again." Both buttons are re-enabled.
- **Focus management:** On modal open, focus is placed on the comment textarea. On modal close (success or cancel), focus returns to the button that triggered it.
- **Backdrop click:** Clicking the backdrop does NOT dismiss the modal (to prevent accidental dismissal mid-action). Only Cancel button or × button close it.
- **Keyboard:** `Escape` key dismisses the modal (same as Cancel). `Enter` in the textarea adds a newline, not submit (prevents accidental submission).

---

### 4.9 Notification Panel

#### Purpose

The Notification Panel displays the current user's in-app notifications — approval requests, approval decisions, and published memo alerts — as a slide-in panel from the right side. It enables quick awareness and navigation to relevant memos without leaving the current page.

#### User Stories Served

- E4-S3: In-App Notification Panel
- E4-S4: Notification Bell Badge

#### ASCII Wireframe

```
┌───────────────────────────────────────────────────────────────────────┐
│ PAGE (dimmed with backdrop when panel is open)                        │
│                                                    ┌──────────────────┤
│                                                    │  NOTIFICATION    │
│                                                    │  PANEL           │
│                                                    │  (offcanvas-end) │
│                                                    │  width: 360px    │
│                                                    │                  │
│                                                    │ HEADER           │
│                                                    │ ┌──────────────┐ │
│                                                    │ │Notifications │ │
│                                                    │ │Mark all read │ │
│                                                    │ │           [×]│ │
│                                                    │ └──────────────┘ │
│                                                    │                  │
│                                                    │ NOTIFICATION LIST│
│                                                    │ ┌──────────────┐ │
│                                                    │ │ ● (unread)   │ │
│                                                    │ │ 🔔 Approval  │ │
│                                                    │ │ Request      │ │
│                                                    │ │              │ │
│                                                    │ │ John Smith   │ │
│                                                    │ │ submitted    │ │
│                                                    │ │ "Q1 Budget   │ │
│                                                    │ │ Policy..."   │ │
│                                                    │ │ for approval.│ │
│                                                    │ │              │ │
│                                                    │ │ 5 minutes ago│ │
│                                                    │ └──────────────┘ │
│                                                    │ ┌──────────────┐ │
│                                                    │ │ ● (unread)   │ │
│                                                    │ │ ✓ Approved   │ │
│                                                    │ │              │ │
│                                                    │ │ Your memo    │ │
│                                                    │ │ "Remote Work │ │
│                                                    │ │ Policy..."   │ │
│                                                    │ │ was approved │ │
│                                                    │ │ by Alice J.  │ │
│                                                    │ │              │ │
│                                                    │ │ 2 hours ago  │ │
│                                                    │ └──────────────┘ │
│                                                    │ ┌──────────────┐ │
│                                                    │ │ (read)       │ │
│                                                    │ │ 📢 Published │ │
│                                                    │ │              │ │
│                                                    │ │ "IT Security │ │
│                                                    │ │ Reminder Q1" │ │
│                                                    │ │ has been     │ │
│                                                    │ │ published.   │ │
│                                                    │ │              │ │
│                                                    │ │ Yesterday    │ │
│                                                    │ └──────────────┘ │
│                                                    │                  │
│                                                    │ EMPTY STATE:     │
│                                                    │ ┌──────────────┐ │
│                                                    │ │   🔔         │ │
│                                                    │ │ No new       │ │
│                                                    │ │ notifications│ │
│                                                    │ └──────────────┘ │
│                                                    │                  │
│                                                    │ FOOTER           │
│                                                    │ ┌──────────────┐ │
│                                                    │ │View all →    │ │
│                                                    │ └──────────────┘ │
│                                                    └──────────────────┤
└───────────────────────────────────────────────────────────────────────┘

NOTIFICATION BELL IN NAVBAR (closed state):
  ┌────┐
  │ 🔔 │  ← icon button
  │  3 │  ← badge bg-danger (hidden when 0)
  └────┘

NOTIFICATION BELL (opened state — panel is visible):
  ┌────┐
  │ 🔔 │  ← icon with active/highlighted state
  │    │  ← badge hidden while panel is open
  └────┘
```

#### Component Inventory

| Component                 | Bootstrap / Blazor Element                       | Notes                                                          |
|---------------------------|--------------------------------------------------|----------------------------------------------------------------|
| Bell button               | `btn btn-outline-light position-relative`        | In top navbar; triggers `offcanvas` open                      |
| Badge                     | `badge bg-danger rounded-pill position-absolute` | Positioned top-right of bell; hidden when count is 0          |
| Panel container           | `offcanvas offcanvas-end`                        | Slides in from right; width 360px; Bootstrap Offcanvas        |
| Panel header              | `offcanvas-header`                               | Title + Mark All Read link + × close button                   |
| Mark all read link        | `btn btn-link btn-sm p-0`                        | Marks all as read in one API call; badge zeroes               |
| Panel body                | `offcanvas-body p-0`                             | Scrollable list of notifications                              |
| Notification item         | `list-group-item list-group-item-action`         | Clickable; navigates to related memo on click                 |
| Unread indicator          | `rounded-circle bg-primary` (small dot)          | 8px dot, position-absolute top-right of item                 |
| Unread item background    | `list-group-item-light`                          | Slightly highlighted background for unread items             |
| Read item background      | Default white                                    | No special background                                         |
| Notification icon         | Bootstrap Icon or SVG                            | Bell (approval request), checkmark (approved), × (rejected), megaphone (published) |
| Message text              | `small`                                          | 2–3 line message; memo title truncated with ellipsis at 40 chars |
| Relative time             | `small text-muted`                               | "5 minutes ago", "2 hours ago", "Yesterday"; computed client-side |
| Empty state               | Centered `div` inside `offcanvas-body`           | Bell icon + "No new notifications"                            |
| View all footer           | `list-group-item text-center`                    | "View all notifications →"; links to dedicated notifications page (post-MVP) |

#### Interaction Notes

- **Opening the panel:** Clicking the bell icon opens the offcanvas from the right side. The page backdrop darkens slightly. The bell badge is hidden while the panel is open.
- **Closing the panel:** Clicking the × button, pressing Escape, or clicking the backdrop closes the panel.
- **Notification click:** Clicking any notification item marks it as read (API call) and navigates to the related memo's detail page (`/memos/{id}`). The panel closes before navigation.
- **Mark all as read:** Clicking "Mark all as read" calls `PATCH /api/notifications/mark-all-read`. All unread dots are removed, the badge zeroes, and the item backgrounds change to white.
- **Real-time updates:** Via Blazor Server's SignalR connection, new notifications push to the client. The badge count increments and the panel list updates without requiring a page reload. A brief animation (flash/bounce) on the bell icon draws attention to the new notification.
- **Notification types and messages:**
  - Approval request: "John Smith submitted 'Q1 Budget Policy Update 2026' for your approval."
  - Approved: "Your memo 'Q4 Expense Report Guidelines' was approved by Alice Jones."
  - Rejected: "Your memo 'Staff Travel Policy v2.1' was rejected by Bob Johnson."
  - Published: "'IT Security Reminder Q1 2026' has been published."
- **Relative time calculation:** Timestamps are rendered as relative time client-side. Notifications older than 7 days show the absolute date.
- **Scroll behavior:** The `offcanvas-body` is scrollable. The header and footer are sticky — always visible regardless of scroll position.
- **Empty state:** When all notifications are read and no new ones exist, the body shows a centered bell icon with "No new notifications" in `text-muted`.

---

### 4.10 Admin Panel

#### Purpose

The Admin Panel provides system administrators with basic user management (view users, change roles) and system configuration display (SMTP settings, file size limits). It is accessible only to users with the Admin role.

#### User Stories Served

- E0-S3: Authentication and Role Management (admin seed user)
- Post-MVP: Formal Roles and Permissions (referenced in requirements section 4.2)

#### ASCII Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│ ADMIN PANEL  (/admin)  — visible only to Admin role                                 │
│                                                                                     │
│  ┌──────────────────┐  ┌──────────────────────────────────────────────────────────┐ │
│  │  ADMIN SIDEBAR   │  │  ADMIN CONTENT AREA                                      │ │
│  │                  │  │                                                          │ │
│  │  Administration  │  │  ══════════════════════════════════════════════          │ │
│  │  ─────────────── │  │  Users                                                   │ │
│  │  👤 Users    ←   │  │  ══════════════════════════════════════════════          │ │
│  │  ⚙  Settings     │  │                                                          │ │
│  │                  │  │  TOOLBAR                                                 │ │
│  └──────────────────┘  │  [+ Invite User]   [🔍 Search users...          ]        │ │
│                        │                                                          │ │
│                        │  USERS TABLE                                             │ │
│                        │  ┌────────────────┬──────────────────────┬──────────┬──────────┬──────────┐ │
│                        │  │ Name          ↕│ Email               ↕│ Role    ↕│ Status  ↕│ Actions  │ │
│                        │  ├────────────────┼──────────────────────┼──────────┼──────────┼──────────┤ │
│                        │  │ Alice Jones    │ alice@corp.com       │ Admin    │ ┌──────┐ │[Edit Role]│ │
│                        │  │                │                      │          │ │Active│ │          │ │
│                        │  │                │                      │          │ └──────┘ │          │ │
│                        │  ├────────────────┼──────────────────────┼──────────┼──────────┼──────────┤ │
│                        │  │ Bob Johnson    │ bob@corp.com         │ User     │ ┌──────┐ │[Edit Role]│ │
│                        │  │                │                      │          │ │Active│ │          │ │
│                        │  │                │                      │          │ └──────┘ │          │ │
│                        │  ├────────────────┼──────────────────────┼──────────┼──────────┼──────────┤ │
│                        │  │ Carol White    │ carol@corp.com       │ User     │ ┌────────┐│[Edit Role]│ │
│                        │  │                │                      │          │ │Inactive││          │ │
│                        │  │                │                      │          │ └────────┘│          │ │
│                        │  └────────────────┴──────────────────────┴──────────┴──────────┴──────────┘ │
│                        │                                                          │ │
│                        │  PAGINATION  [«] [1] [2] [3] [»]  |  Rows: [25 ▾]       │ │
│                        │                                                          │ │
└─────────────────────────────────────────────────────────────────────────────────┘

EDIT ROLE MODAL:
┌────────────────────────────────────────────┐
│  Edit Role                           [×]   │
│  ────────────────────────────────────────  │
│  User: Bob Johnson (bob@corp.com)          │
│                                            │
│  Role:   [User          ▾]                 │
│          Options: Admin / User             │
│                                            │
│  [Cancel]         [Save Changes]           │
└────────────────────────────────────────────┘

SYSTEM SETTINGS PAGE (/admin/settings):
┌─────────────────────────────────────────────────────────────────┐
│  ══════════════════════════════════════════════                  │
│  System Settings                                                 │
│  ══════════════════════════════════════════════                  │
│                                                                  │
│  FILE UPLOAD LIMITS                                              │
│  ────────────────────────────────────────────                    │
│  Max file size per attachment:   10240 KB  (10 MB)               │
│  Max total attachments per memo: 51200 KB  (50 MB)               │
│  Allowed file types:             PDF, DOCX, XLSX, PNG, JPG,      │
│                                  JPEG, GIF, BMP                  │
│                                                                  │
│  SMTP SETTINGS                                                   │
│  ────────────────────────────────────────────                    │
│  SMTP Host:      smtp.corp.internal                              │
│  SMTP Port:      587                                             │
│  From Address:   no-reply@corp.com                               │
│  TLS Enabled:    Yes                                             │
│  Credentials:    *** (hidden for security)                       │
│                                                                  │
│  ℹ  Settings are read from appsettings.json.                     │
│     To change settings, update the configuration file           │
│     and restart the application.                                 │
│                                                                  │
│  APPLICATION                                                     │
│  ────────────────────────────────────────────                    │
│  Version:        1.0.0                                           │
│  Environment:    Production                                      │
│  .NET Runtime:   8.0.x                                           │
└─────────────────────────────────────────────────────────────────┘
```

#### Component Inventory

| Component                 | Bootstrap / Blazor Element                       | Notes                                                          |
|---------------------------|--------------------------------------------------|----------------------------------------------------------------|
| Admin route guard         | `[Authorize(Roles = "Admin")]`                   | Blazor `AuthorizeView` or page-level attribute                |
| Admin sidebar             | `nav nav-pills flex-column`                      | Nested within the main layout's sidebar; Admin section         |
| Users table               | `table table-hover table-bordered`               | Same table pattern as Dashboard                                |
| Name / Email / Role cols  | Standard `<td>`                                  | Sortable headers                                               |
| Status badge (active)     | `badge bg-success`                               | "Active" in green                                              |
| Status badge (inactive)   | `badge bg-secondary`                             | "Inactive" in gray                                             |
| Edit Role button          | `btn btn-sm btn-outline-secondary`               | Opens Edit Role modal                                          |
| Edit Role modal           | `modal modal-dialog-centered modal-sm`           | Small modal; role dropdown + save button                       |
| Role dropdown             | `<select>` / `form-select`                       | Options: Admin, User                                           |
| Save Changes button       | `btn btn-primary`                                | Calls `PUT /api/admin/users/{id}/role`                        |
| Settings page             | Static read-only display                         | `dl dt dd` definition list or a table layout                  |
| Settings values           | `form-control-plaintext`                         | Read-only; password/credentials shown as `***`                |
| Settings info alert       | `alert alert-info`                               | Notes that settings require config file change + restart      |

#### Interaction Notes

- **Route protection:** The entire `/admin` route subtree is protected by `[Authorize(Roles = "Admin")]`. Users without the Admin role who attempt to navigate to `/admin` receive a 403 Forbidden response and are redirected to the Dashboard with a `toast alert-warning`: "You do not have permission to access the Admin Panel."
- **Edit Role modal:** Opens with the user's current role pre-selected in the dropdown. Saving calls `PUT /api/admin/users/{id}/role`. On success, the table row updates reactively and a success toast appears: "Role updated for Bob Johnson."
- **Invite User button:** Shown in toolbar but marked "Coming soon" with a tooltip in MVP — user provisioning is admin-only via direct database seeding in the MVP (constraint A1).
- **Settings page:** All values are read-only display. Settings are sourced from `IConfiguration` (i.e., `appsettings.json` or environment variables). Sensitive values (SMTP password, connection strings) are masked as `***`. The info alert clarifies that changes require a configuration file update and application restart.
- **Inactive users:** Users with `LockoutEnd > DateTime.UtcNow` (ASP.NET Core Identity lockout) are shown as "Inactive". Re-activation is a post-MVP feature; no button is shown in MVP.
- **User search:** The search input filters the users table client-side (for small user counts) or server-side via `GET /api/admin/users?q=...` for larger directories.
- **Responsive behavior:** The admin sidebar collapses into the main sidebar on mobile (Admin section headings remain visible). The users table uses `table-responsive` for horizontal scroll on narrow viewports.

---

*End of UX Design Document*
