using CorporateMemo.Domain.Entities;

namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Defines the contract for sending email notifications from the system.
/// Email sending is asynchronous and failures are logged but do not roll back primary operations.
/// This interface is implemented in the Infrastructure layer using MailKit/MimeKit.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends approval request emails to all assigned approvers when a memo is submitted.
    /// Each email contains the memo title, number, author name, and approve/reject action links.
    /// </summary>
    /// <param name="memo">The memo that has been submitted for approval.</param>
    /// <param name="baseUrl">
    /// The base URL of the application (e.g., "https://memos.company.com").
    /// Used to construct the direct link to the memo's approval page in the email.
    /// </param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task SendApprovalRequestAsync(Memo memo, string baseUrl, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification email to the memo author when an approver makes a decision.
    /// The email includes the approver's name, decision, timestamp, and any rejection comment.
    /// </summary>
    /// <param name="memo">The memo whose approval decision has been recorded.</param>
    /// <param name="approverName">The display name of the approver who made the decision.</param>
    /// <param name="approved">True if the approver approved; false if they rejected.</param>
    /// <param name="comment">Optional rejection comment from the approver.</param>
    /// <param name="baseUrl">The base URL of the application for constructing memo links.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task SendApprovalDecisionAsync(Memo memo, string approverName, bool approved, string? comment, string baseUrl, CancellationToken ct = default);

    /// <summary>
    /// Sends publication notification emails to all To and CC distribution list recipients.
    /// The email includes the memo title, number, author, and a link to view the memo.
    /// </summary>
    /// <param name="memo">The memo that has been published.</param>
    /// <param name="baseUrl">The base URL of the application for constructing memo links.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task SendMemoPublishedAsync(Memo memo, string baseUrl, CancellationToken ct = default);
}
