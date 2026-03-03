using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to update an existing memo.
/// Only memos in Draft or Rejected status can be updated.
/// Only the memo's author can update it.
/// Handled by <see cref="UpdateMemoCommandHandler"/>.
/// </summary>
public class UpdateMemoCommand : IRequest<MemoDto>
{
    /// <summary>Gets or sets the ID of the memo to update. Required.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets the updated title. Maximum 100 characters. Required.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated body content. Maximum 10,000 characters.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated list of hashtag keywords.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the updated primary recipients (To field) as email addresses.</summary>
    public List<string> ToRecipients { get; set; } = new();

    /// <summary>Gets or sets the updated CC recipients as email addresses.</summary>
    public List<string> CcRecipients { get; set; } = new();

    /// <summary>Gets or sets the updated list of approvers.</summary>
    public List<ApproverInfo> Approvers { get; set; } = new();

    /// <summary>Gets or sets whether this memo should be marked as confidential.</summary>
    public bool IsConfidential { get; set; }
}
