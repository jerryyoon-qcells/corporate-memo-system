using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="SubmitMemoCommand"/> by transitioning a memo from Draft/Rejected to
/// either PendingApproval (if approvers are assigned) or Published (if no approvers).
///
/// Business rules enforced:
/// - Memo must be in Draft or Rejected status to be submitted
/// - Only the author can submit their own memo
/// - At least one To recipient is required
/// - If approvers exist: status becomes PendingApproval and approval request emails are sent
/// - If no approvers: status becomes Published and distribution emails are sent
/// </summary>
public class SubmitMemoCommandHandler : IRequestHandler<SubmitMemoCommand, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;
    private readonly ILogger<SubmitMemoCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public SubmitMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IEmailService emailService,
        INotificationService notificationService,
        IMapper mapper,
        ILogger<SubmitMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _emailService = emailService;
        _notificationService = notificationService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the submit memo command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the current user is not the author.</exception>
    /// <exception cref="InvalidMemoStateException">Thrown if the memo is not submittable.</exception>
    public async Task<MemoDto> Handle(SubmitMemoCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to submit a memo.");

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Only the author can submit their memo
        if (memo.AuthorId != userId)
            throw new UnauthorizedMemoAccessException(memo.Id, userId, "Submit");

        // Only Draft or Rejected memos can be submitted
        if (memo.Status != MemoStatus.Draft && memo.Status != MemoStatus.Rejected)
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Submit",
                "Only Draft or Rejected memos can be submitted.");

        // At least one To recipient is required before submission
        if (!memo.ToRecipients.Any())
            throw new InvalidMemoStateException(memo.Id, memo.Status, "Submit",
                "At least one recipient in the To field is required to submit a memo.");

        // Determine workflow path based on whether approvers are assigned
        var hasApprovers = memo.ApprovalSteps.Any();

        if (hasApprovers)
        {
            // Approval workflow: transition to PendingApproval
            _logger.LogInformation("Submitting memo {MemoId} for approval to {ApproverCount} approver(s)",
                memo.Id, memo.ApprovalSteps.Count);

            memo.Status = MemoStatus.PendingApproval;
            await _memoRepository.UpdateAsync(memo, cancellationToken);

            // Send approval request emails to all approvers (fire-and-forget logging on failure)
            try
            {
                await _emailService.SendApprovalRequestAsync(memo, request.BaseUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                // Email failure must not roll back the primary operation per requirements
                _logger.LogError(ex, "Failed to send approval request emails for memo {MemoId}", memo.Id);
            }

            // Create in-app notifications for each approver
            foreach (var step in memo.ApprovalSteps)
            {
                try
                {
                    await _notificationService.CreateNotificationAsync(
                        step.ApproverId,
                        $"Memo '{memo.MemoNumber}' by {memo.AuthorName} requires your approval.",
                        NotificationType.ApprovalRequested,
                        memo.Id,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create notification for approver {ApproverId}", step.ApproverId);
                }
            }
        }
        else
        {
            // Auto-publish: no approvers assigned, publish immediately
            _logger.LogInformation("Auto-publishing memo {MemoId} — no approvers assigned", memo.Id);

            memo.Status = MemoStatus.Published;
            memo.ApprovedDate = DateTime.UtcNow;
            await _memoRepository.UpdateAsync(memo, cancellationToken);

            // Send publication notification to distribution list
            try
            {
                await _emailService.SendMemoPublishedAsync(memo, request.BaseUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send publication emails for memo {MemoId}", memo.Id);
            }
        }

        return _mapper.Map<MemoDto>(memo);
    }
}
