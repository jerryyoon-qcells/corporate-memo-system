using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to submit a memo for approval (or auto-publish if no approvers are assigned).
///
/// Business rules:
/// - If the memo has approvers: status → PendingApproval, emails sent to all approvers
/// - If no approvers: status → Published immediately, emails sent to distribution list
/// - At least one To recipient is required for submission
///
/// Handled by <see cref="SubmitMemoCommandHandler"/>.
/// </summary>
public class SubmitMemoCommand : IRequest<MemoDto>
{
    /// <summary>Gets or sets the ID of the memo to submit.</summary>
    public Guid MemoId { get; set; }

    /// <summary>
    /// Gets or sets the base URL of the application.
    /// Used to construct approve/reject links in notification emails.
    /// Example: "https://memos.company.com"
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
