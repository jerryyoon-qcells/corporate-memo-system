using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command for an approver to reject a memo.
/// Rejection by any approver immediately transitions the memo to Rejected status.
/// The author is notified with the rejection comment.
/// Handled by <see cref="RejectMemoCommandHandler"/>.
/// </summary>
public class RejectMemoCommand : IRequest<MemoDto>
{
    /// <summary>Gets or sets the ID of the memo to reject.</summary>
    public Guid MemoId { get; set; }

    /// <summary>
    /// Gets or sets the rejection comment from the approver.
    /// This is displayed to the author so they understand why the memo was rejected.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>Gets or sets the base URL for constructing links in notification emails.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
