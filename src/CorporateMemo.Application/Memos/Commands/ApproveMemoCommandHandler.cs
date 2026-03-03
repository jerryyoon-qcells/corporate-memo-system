using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="ApproveMemoCommand"/> by recording the approver's decision.
///
/// Business rules enforced:
/// - Memo must be in PendingApproval status
/// - Current user must be an assigned approver for this memo
/// - The approver must not have already decided
/// - If all approvers have now approved: memo transitions directly to Published
/// - If at least one approver has not yet decided: memo remains in PendingApproval
/// - Notifies the author of the decision
///
/// Note on status transitions (M4 fix):
/// <c>MemoStatus.Approved</c> is NOT used as a memo lifecycle state. The individual
/// per-approver decision is recorded as <c>ApprovalDecision.Approved</c>. When all
/// approvers have approved, the memo moves from PendingApproval directly to Published.
/// </summary>
public class ApproveMemoCommandHandler : IRequestHandler<ApproveMemoCommand, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<ApproveMemoCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public ApproveMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IEmailService emailService,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<ApproveMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _emailService = emailService;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the approve memo command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the user is not an assigned approver.</exception>
    /// <exception cref="InvalidMemoStateException">Thrown if the memo is not in PendingApproval status.</exception>
    public async Task<MemoDto> Handle(ApproveMemoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to approve a memo.");

        var approverName = _currentUser.DisplayName ?? _currentUser.UserName ?? userId;

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Memo must be in PendingApproval status
        if (memo.Status != MemoStatus.PendingApproval)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Approve",
                "Only memos in Pending Approval status can be approved.");

        // Find the current user's approval step
        var approvalStep = memo.ApprovalSteps.FirstOrDefault(s => s.ApproverId == userId)
            ?? throw new UnauthorizedMemoAccessException(memo.Id, userId, "Approve");

        // Prevent double-voting
        if (approvalStep.Decision != ApprovalDecision.Pending)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Approve",
                "You have already made a decision on this memo.");

        // Record the approval decision
        approvalStep.Decision = ApprovalDecision.Approved;
        approvalStep.DecidedAt = DateTime.UtcNow;
        approvalStep.Comment = request.Comment;

        _logger.LogInformation("Approver {ApproverId} approved memo {MemoId}", userId, memo.Id);

        // Check if ALL approvers have now approved
        var allApproved = memo.ApprovalSteps.All(s => s.Decision == ApprovalDecision.Approved);

        if (allApproved)
        {
            // M4 fix: The MemoStatus.Approved enum value is for ApprovalDecision (per-approver),
            // NOT for MemoStatus. The memo transitions directly from PendingApproval to Published
            // once all individual approvers have set their ApprovalDecision to Approved.
            // The intermediate memo.Status = MemoStatus.Approved assignment that previously
            // appeared here was immediately overwritten and never persisted — it has been removed.
            _logger.LogInformation("All approvers approved memo {MemoId} — publishing", memo.Id);
            memo.ApprovedDate = DateTime.UtcNow;
            memo.Status = MemoStatus.Published;
        }

        // Persist the updated memo
        await _memoRepository.UpdateAsync(memo, cancellationToken);

        // Notify the author of the approval decision
        try
        {
            await _emailService.SendApprovalDecisionAsync(memo, approverName, true, request.Comment, request.BaseUrl, cancellationToken);
            await _notificationService.CreateNotificationAsync(
                memo.AuthorId,
                $"Your memo '{memo.MemoNumber}' was approved by {approverName}.",
                NotificationType.ApprovalDecision,
                memo.Id,
                cancellationToken);

            // If fully published, notify distribution list
            if (allApproved)
            {
                await _emailService.SendMemoPublishedAsync(memo, request.BaseUrl, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send approval notification for memo {MemoId}", memo.Id);
        }

        return _mapper.Map<MemoDto>(memo);
    }
}
