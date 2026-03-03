using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Domain.Entities;

/// <summary>
/// Represents a single approver's step in the approval workflow for a memo.
/// Each assigned approver gets one ApprovalStep record that tracks their individual decision.
/// </summary>
public class ApprovalStep
{
    /// <summary>
    /// Gets or sets the unique identifier for this approval step.
    /// A new Guid is assigned automatically when the step is created.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the identifier of the memo this approval step belongs to.
    /// This is a foreign key linking back to the Memo entity.
    /// </summary>
    public Guid MemoId { get; set; }

    /// <summary>
    /// Gets or sets the unique user ID of the approver (from ASP.NET Core Identity).
    /// This maps to the ApplicationUser.Id field.
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the approver (e.g., "Jane Smith").
    /// Stored here so it remains accurate even if the user's name changes later.
    /// </summary>
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the approver.
    /// Used to send approval request notifications via email.
    /// </summary>
    public string ApproverEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sequential order of this approver in the workflow.
    /// For MVP, all approvers are notified in parallel regardless of order.
    /// This field is reserved for future sequential approval chain support.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the approver's current decision on this memo.
    /// Starts as Pending and changes to Approved or Rejected when the approver acts.
    /// </summary>
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;

    /// <summary>
    /// Gets or sets the UTC timestamp when the approver made their decision.
    /// Null if the approver has not yet decided (Decision is Pending).
    /// </summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>
    /// Gets or sets an optional comment left by the approver when making their decision.
    /// Required when rejecting; optional when approving.
    /// </summary>
    public string? Comment { get; set; }

    // Navigation property: allows EF Core to load the related Memo in the same query
    /// <summary>
    /// Gets or sets the memo that owns this approval step.
    /// Navigation property used by Entity Framework Core for related data loading.
    /// </summary>
    public Memo? Memo { get; set; }
}
