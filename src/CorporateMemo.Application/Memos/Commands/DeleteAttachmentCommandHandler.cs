using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="DeleteAttachmentCommand"/> by removing a file attachment
/// from both the storage system and the database.
/// </summary>
public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Unit>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly ILogger<DeleteAttachmentCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public DeleteAttachmentCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IAttachmentStorage attachmentStorage,
        ILogger<DeleteAttachmentCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _attachmentStorage = attachmentStorage;
        _logger = logger;
    }

    /// <summary>Handles the delete attachment command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the user is not the author.</exception>
    public async Task<Unit> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to delete attachments.");

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Only the author (or admin) can delete attachments
        if (memo.AuthorId != userId && !_currentUser.IsAdmin)
            throw new UnauthorizedMemoAccessException(memo.Id, userId, "Delete Attachment");

        // Find the specific attachment to delete
        var attachment = memo.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
        if (attachment == null)
        {
            // Attachment not found — log and return success (idempotent delete)
            _logger.LogWarning("Attachment {AttachmentId} not found on memo {MemoId}", request.AttachmentId, memo.Id);
            return Unit.Value;
        }

        // Delete the physical file from storage
        try
        {
            await _attachmentStorage.DeleteAsync(attachment.StoragePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {StoragePath} for attachment {AttachmentId}",
                attachment.StoragePath, attachment.Id);
            // Continue with database deletion even if file deletion fails
        }

        // Remove the attachment record from the memo and update the database
        memo.Attachments.Remove(attachment);
        await _memoRepository.UpdateAsync(memo, cancellationToken);

        _logger.LogInformation("Attachment {AttachmentId} deleted from memo {MemoId}", attachment.Id, memo.Id);

        return Unit.Value;
    }
}
