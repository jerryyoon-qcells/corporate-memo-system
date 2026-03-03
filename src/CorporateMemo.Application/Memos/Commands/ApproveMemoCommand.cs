using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command for an approver to approve a memo.
/// If this is the final pending approval, the memo transitions to Approved then Published.
/// Handled by <see cref="ApproveMemoCommandHandler"/>.
/// </summary>
public class ApproveMemoCommand : IRequest<MemoDto>
{
    /// <summary>Gets or sets the ID of the memo to approve.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets an optional comment from the approver.</summary>
    public string? Comment { get; set; }

    /// <summary>Gets or sets the base URL for constructing links in notification emails.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
