using MediatR;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Command to delete a file attachment from a memo.
/// Removes both the file from storage and the attachment record from the database.
/// Handled by <see cref="DeleteAttachmentCommandHandler"/>.
/// </summary>
public class DeleteAttachmentCommand : IRequest<Unit>
{
    /// <summary>Gets or sets the ID of the memo that owns the attachment.</summary>
    public Guid MemoId { get; set; }

    /// <summary>Gets or sets the ID of the attachment to delete.</summary>
    public Guid AttachmentId { get; set; }
}
