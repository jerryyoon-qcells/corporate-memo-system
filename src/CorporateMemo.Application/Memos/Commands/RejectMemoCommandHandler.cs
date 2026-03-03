using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="RejectMemoCommand"/> by recording the rejection decision
/// and transitioning the memo to Rejected status.
///
/// Business rules enforced:
/// - Memo must be in PendingApproval status
/// - Current user must be an assigned approver for this memo
/// - Rejection by any single approver immediately rejects the entire memo
/// - The author is notified with the rejection comment
/// </summary>
public class RejectMemoCommandHandler : IRequestHandler<RejectMemoCommand, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<RejectMemoCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public RejectMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IEmailService emailService,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<RejectMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _emailService = emailService;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the reject memo command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the user is not an assigned approver.</exception>
    /// <exception cref="InvalidMemoStateException">Thrown if the memo is not in PendingApproval status.</exception>
    public async Task<MemoDto> Handle(RejectMemoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to reject a memo.");

        var approverName = _currentUser.DisplayName ?? _currentUser.UserName ?? userId;

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Memo must be in PendingApproval status
        if (memo.Status != MemoStatus.PendingApproval)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Reject",
                "Only memos in Pending Approval status can be rejected.");

        // Find the current user's approval step
        var approvalStep = memo.ApprovalSteps.FirstOrDefault(s => s.ApproverId == userId)
            ?? throw new UnauthorizedMemoAccessException(memo.Id, userId, "Reject");

        // Prevent double-voting
        if (approvalStep.Decision != ApprovalDecision.Pending)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Reject",
                "You have already made a decision on this memo.");

        // Record the rejection decision on this specific approver's step
        approvalStep.Decision = ApprovalDecision.Rejected;
        approvalStep.DecidedAt = DateTime.UtcNow;
        approvalStep.Comment = request.Comment;

        // Any single rejection immediately rejects the entire memo
        memo.Status = MemoStatus.Rejected;

        _logger.LogInformation("Approver {ApproverId} rejected memo {MemoId}", userId, memo.Id);

        // Persist the rejection
        await _memoRepository.UpdateAsync(memo, cancellationToken);

        // Notify the author of the rejection
        try
        {
            await _emailService.SendApprovalDecisionAsync(
                memo, approverName, false, request.Comment, request.BaseUrl, cancellationToken);

            await _notificationService.CreateNotificationAsync(
                memo.AuthorId,
                $"Your memo '{memo.MemoNumber}' was rejected by {approverName}." +
                (string.IsNullOrEmpty(request.Comment) ? "" : $" Comment: {request.Comment}"),
                NotificationType.ApprovalDecision,
                memo.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send rejection notification for memo {MemoId}", memo.Id);
        }

        return _mapper.Map<MemoDto>(memo);
    }
}
