using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to permanently delete a memo.
/// Only memos in Draft status can be deleted (business rule: published memos cannot be removed).
/// Only the memo author can delete their own memo.
/// Handled by <see cref="DeleteMemoCommandHandler"/>.
/// </summary>
public class DeleteMemoCommand : IRequest<Unit>
{
    /// <summary>Gets or sets the ID of the memo to delete.</summary>
    public Guid MemoId { get; set; }
}
