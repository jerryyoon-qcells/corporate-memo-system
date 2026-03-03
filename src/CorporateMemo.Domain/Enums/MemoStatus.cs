namespace CorporateMemo.Domain.Enums;

/// <summary>
/// Represents the lifecycle status of a memo document.
/// Each memo progresses through these statuses as it moves through the workflow.
/// </summary>
public enum MemoStatus
{
    /// <summary>
    /// The memo has been started but not yet submitted.
    /// The author can still edit or delete the memo in this state.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// The memo has been submitted and is awaiting approval from one or more approvers.
    /// The author cannot edit the memo while it is in this state.
    /// </summary>
    PendingApproval = 1,

    /// <summary>
    /// All assigned approvers have approved the memo.
    /// The memo is immediately transitioned to Published after this state.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// At least one approver has rejected the memo.
    /// The author can edit and re-submit the memo from this state.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// The memo has been published and is visible to all users in the "All Documents" tab.
    /// Distribution list recipients have been notified by email.
    /// </summary>
    Published = 4
}
