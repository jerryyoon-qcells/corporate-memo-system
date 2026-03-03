using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to create a new memo in Draft status.
/// Commands in CQRS represent intentions to change state.
/// This command contains all the data needed to create the memo.
/// It is handled by <see cref="CreateMemoCommandHandler"/>.
/// </summary>
public class CreateMemoCommand : IRequest<MemoDto>
{
    /// <summary>Gets or sets the title of the memo. Maximum 100 characters. Required.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the body content of the memo. Maximum 1000 characters of plain text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the list of hashtag keywords for this memo.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the primary recipients (To field) as email addresses.</summary>
    public List<string> ToRecipients { get; set; } = new();

    /// <summary>Gets or sets the CC recipients as email addresses. Optional.</summary>
    public List<string> CcRecipients { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of approver information (user ID, name, email) for the approval workflow.
    /// If empty, submitting this memo will auto-publish it.
    /// </summary>
    public List<ApproverInfo> Approvers { get; set; } = new();

    /// <summary>Gets or sets whether this memo should be marked as confidential.</summary>
    public bool IsConfidential { get; set; }
}

/// <summary>
/// Represents information about an approver to be assigned to a memo.
/// Used within commands where approvers need to be specified.
/// </summary>
public class ApproverInfo
{
    /// <summary>Gets or sets the unique user ID of the approver.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the approver (e.g., "Jane Smith").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the approver.</summary>
    public string Email { get; set; } = string.Empty;
}
