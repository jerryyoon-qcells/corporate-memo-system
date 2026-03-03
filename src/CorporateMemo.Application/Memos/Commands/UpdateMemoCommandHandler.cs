using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="UpdateMemoCommand"/> by updating an existing memo's fields.
///
/// Business rules enforced:
/// - Only the memo author can update it
/// - Only Draft or Rejected memos can be updated (not Pending, Approved, or Published)
/// - Approver assignments are replaced (not merged) with the new list
/// </summary>
public class UpdateMemoCommandHandler : IRequestHandler<UpdateMemoCommand, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateMemoCommandHandler> _logger;

    /// <summary>Initializes the handler with required dependencies.</summary>
    public UpdateMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<UpdateMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the update command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the current user is not the author.</exception>
    /// <exception cref="InvalidMemoStateException">Thrown if the memo is not in Draft or Rejected status.</exception>
    public async Task<MemoDto> Handle(UpdateMemoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to update a memo.");

        // Load the memo from the database (including ApprovalSteps so we can replace them)
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Only the author can edit their own memo
        if (memo.AuthorId != userId && !_currentUser.IsAdmin)
            throw new UnauthorizedMemoAccessException(memo.Id, userId, "Update");

        // Only Draft or Rejected memos can be updated
        // A memo in Pending/Approved/Published state is locked for editing
        if (memo.Status != MemoStatus.Draft && memo.Status != MemoStatus.Rejected)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Update",
                $"Only Draft or Rejected memos can be edited. Current status: {memo.Status}.");

        _logger.LogInformation("Updating memo {MemoId} for user {UserId}", memo.Id, userId);

        // Update the memo's editable fields
        memo.Title = request.Title;
        memo.Content = request.Content;
        memo.Tags = request.Tags;
        memo.ToRecipients = request.ToRecipients;
        memo.CcRecipients = request.CcRecipients;
        memo.IsConfidential = request.IsConfidential;

        // Replace the approval steps with the new list
        // This handles the case where approvers are added, removed, or changed
        memo.ApprovalSteps.Clear();
        var order = 1;
        foreach (var approverInfo in request.Approvers)
        {
            memo.ApprovalSteps.Add(new ApprovalStep
            {
                Id = Guid.NewGuid(),
                MemoId = memo.Id,
                ApproverId = approverInfo.UserId,
                ApproverName = approverInfo.DisplayName,
                ApproverEmail = approverInfo.Email,
                Order = order++,
                Decision = ApprovalDecision.Pending
            });
        }

        // Persist the changes to the database
        await _memoRepository.UpdateAsync(memo, cancellationToken);

        _logger.LogInformation("Memo {MemoId} updated successfully", memo.Id);

        return _mapper.Map<MemoDto>(memo);
    }
}
