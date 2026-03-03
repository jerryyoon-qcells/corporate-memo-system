using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.DTOs;

/// <summary>
/// Data Transfer Object for approval steps in the approval workflow.
/// Represents one approver's participation and decision on a specific memo.
/// Displayed in the Approval History timeline on the Memo Detail page.
/// </summary>
public class ApprovalStepDto
{
    /// <summary>Gets or sets the unique identifier of this approval step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the ID of the memo this step belongs to.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets the user ID of the approver.</summary>
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the approver (e.g., "Jane Smith").</summary>
    public string ApproverName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the approver.</summary>
    public string ApproverEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the order of this approver in the workflow (reserved for future sequential approval).</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets the approver's current decision.</summary>
    public ApprovalDecision Decision { get; set; }

    /// <summary>
    /// Gets a display-friendly string for the decision.
    /// Returns "Approved", "Rejected", or "Pending" based on the Decision enum value.
    /// </summary>
    public string DecisionDisplay => Decision switch
    {
        ApprovalDecision.Approved => "Approved",
        ApprovalDecision.Rejected => "Rejected",
        _ => "Pending"
    };

    /// <summary>Gets or sets the UTC timestamp when the decision was made. Null if still pending.</summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>Gets or sets the optional comment left by the approver.</summary>
    public string? Comment { get; set; }
}
