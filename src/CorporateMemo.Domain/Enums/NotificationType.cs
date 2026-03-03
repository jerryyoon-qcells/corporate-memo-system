namespace CorporateMemo.Domain.Enums;

/// <summary>
/// Represents the type of in-app notification sent to a user.
/// This determines the notification message text and the action when clicked.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Sent to approvers when a memo is submitted and requires their approval.
    /// Clicking this notification navigates to the memo's approval view.
    /// </summary>
    ApprovalRequested = 0,

    /// <summary>
    /// Sent to the memo author when an approver makes a decision (approved or rejected).
    /// Clicking this notification navigates to the memo detail view.
    /// </summary>
    ApprovalDecision = 1,

    /// <summary>
    /// Sent to distribution list recipients when a memo is published.
    /// Clicking this notification navigates to the memo detail view.
    /// </summary>
    MemoPublished = 2
}
