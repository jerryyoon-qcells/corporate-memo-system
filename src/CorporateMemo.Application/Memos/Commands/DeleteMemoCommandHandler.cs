using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="DeleteMemoCommand"/> by permanently deleting a Draft memo.
///
/// Business rules enforced:
/// - Only Draft memos can be deleted
/// - Only the author can delete their own memo
/// - Related attachments are also cleaned up from storage
/// </summary>
public class DeleteMemoCommandHandler : IRequestHandler<DeleteMemoCommand, Unit>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly ILogger<DeleteMemoCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public DeleteMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IAttachmentStorage attachmentStorage,
        ILogger<DeleteMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _attachmentStorage = attachmentStorage;
        _logger = logger;
    }

    /// <summary>Handles the delete memo command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the current user is not the author.</exception>
    /// <exception cref="InvalidMemoStateException">Thrown if the memo is not in Draft status.</exception>
    public async Task<Unit> Handle(DeleteMemoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to delete a memo.");

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Only the author can delete their memo (unless admin)
        if (memo.AuthorId != userId && !_currentUser.IsAdmin)
            throw new UnauthorizedMemoAccessException(memo.Id, userId, "Delete");

        // Only Draft memos can be deleted — published/approved memos must be retained
        if (memo.Status != MemoStatus.Draft)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Delete",
                "Only Draft memos can be deleted.");

        // Clean up file attachments from storage before deleting the database record
        foreach (var attachment in memo.Attachments)
        {
            try
            {
                await _attachmentStorage.DeleteAsync(attachment.StoragePath, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log but don't abort the delete if file cleanup fails
                // The database record should still be deleted even if the file is orphaned
                _logger.LogWarning(ex, "Failed to delete attachment file {StoragePath}", attachment.StoragePath);
            }
        }

        _logger.LogInformation("Deleting Draft memo {MemoId} for user {UserId}", memo.Id, userId);

        // Delete the memo record (cascade delete handles ApprovalSteps and Attachments records)
        await _memoRepository.DeleteAsync(memo, cancellationToken);

        _logger.LogInformation("Memo {MemoId} deleted successfully", request.MemoId);

        // MediatR requires a return value; Unit is the MediatR equivalent of void
        return Unit.Value;
    }
}
