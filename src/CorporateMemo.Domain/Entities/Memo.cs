using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Domain.Entities;

/// <summary>
/// Represents a corporate memo document — the central entity of this system.
/// A memo is created by an author, goes through an approval workflow, and is eventually published
/// to a distribution list of recipients.
/// </summary>
public class Memo
{
    /// <summary>
    /// Gets or sets the unique identifier for this memo.
    /// A new Guid is assigned automatically when the memo is created.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the human-readable memo number in the format [username]-[YYYYMMDD]-[seq].
    /// Example: "jsmith-20260302-001". Auto-generated on first save by MemoNumberGenerator.
    /// </summary>
    public string MemoNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the title of the memo. Maximum 100 characters. Required.
    /// This is what users see in the dashboard list view.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique user ID of the memo author (from ASP.NET Core Identity).
    /// This maps to the ApplicationUser.Id field.
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the author (e.g., "John Smith").
    /// Stored at creation time so it remains accurate even if the user's profile changes.
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the author.
    /// Used to send notifications (approval decisions, publication confirmation) to the author.
    /// </summary>
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this memo was first created.
    /// Set automatically at creation and never changed afterward.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when this memo was fully approved.
    /// Null if the memo has not yet been approved.
    /// Set automatically when the final approver approves the memo.
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Gets or sets the body content of the memo. Maximum 1000 characters of plain text equivalent.
    /// Supports rich text formatting (bold, italic, lists, links).
    /// Content must be sanitised before storage to prevent XSS attacks.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current lifecycle status of the memo.
    /// Starts as Draft and progresses through the workflow states.
    /// </summary>
    public MemoStatus Status { get; set; } = MemoStatus.Draft;

    /// <summary>
    /// Gets or sets whether this memo is marked as confidential.
    /// The confidential flag is stored and displayed in the MVP.
    /// Full access control enforcement based on this flag is deferred to post-MVP.
    /// </summary>
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Gets or sets the list of hashtag keywords for categorisation and search.
    /// Maximum 20 tags per the UX design. Stored as a JSON column in SQL Server.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the primary recipients (To field) of this memo as email addresses.
    /// At least one To recipient is required when submitting for approval or publishing.
    /// Stored as a JSON column in SQL Server.
    /// </summary>
    public List<string> ToRecipients { get; set; } = new();

    /// <summary>
    /// Gets or sets the CC recipients of this memo as email addresses.
    /// Optional. Stored as a JSON column in SQL Server.
    /// </summary>
    public List<string> CcRecipients { get; set; } = new();

    /// <summary>
    /// Gets or sets the file attachments associated with this memo.
    /// Navigation property - EF Core loads these via Include() to avoid N+1 queries.
    /// </summary>
    public List<Attachment> Attachments { get; set; } = new();

    /// <summary>
    /// Gets or sets the approval steps for this memo — one per assigned approver.
    /// Navigation property - EF Core loads these via Include() to avoid N+1 queries.
    /// </summary>
    public List<ApprovalStep> ApprovalSteps { get; set; } = new();
}
