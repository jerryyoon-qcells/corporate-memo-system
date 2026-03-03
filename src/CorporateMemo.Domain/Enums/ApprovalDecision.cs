namespace CorporateMemo.Domain.Enums;

/// <summary>
/// Represents the decision made by an approver on a memo.
/// Each approver in the ApprovalStep has one of these decisions.
/// </summary>
public enum ApprovalDecision
{
    /// <summary>
    /// The approver has not yet made a decision.
    /// This is the initial state when a memo is submitted for approval.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The approver has approved the memo.
    /// When all approvers approve, the memo transitions to Published.
    /// </summary>
    Approved = 1,

    /// <summary>
    /// The approver has rejected the memo.
    /// A single rejection causes the entire memo to be rejected.
    /// </summary>
    Rejected = 2
}
