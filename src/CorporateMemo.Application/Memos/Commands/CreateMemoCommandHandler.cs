using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="CreateMemoCommand"/> by creating a new memo in Draft status.
///
/// Responsibility of this handler:
/// 1. Validate that the current user is authenticated
/// 2. Generate the memo number using MemoNumberGenerator
/// 3. Create the Memo entity with all provided data
/// 4. Create ApprovalStep records for each assigned approver
/// 5. Persist the memo via the repository
/// 6. Return the created memo as a MemoDto
/// </summary>
public class CreateMemoCommandHandler : IRequestHandler<CreateMemoCommand, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateMemoCommandHandler> _logger;

    /// <summary>
    /// Initializes the handler with all required dependencies.
    /// Dependencies are injected via the DI container at runtime.
    /// </summary>
    public CreateMemoCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<CreateMemoCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the create memo command. Creates a new memo in Draft status.
    /// </summary>
    /// <param name="request">The create memo command with all memo data.</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed.</param>
    /// <returns>The newly created memo as a MemoDto.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current user is not authenticated.</exception>
    public async Task<MemoDto> Handle(CreateMemoCommand request, CancellationToken cancellationToken)
    {
        // Ensure a user is authenticated before creating a memo
        // Anonymous memo creation is not allowed
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to create a memo.");

        var userName = _currentUser.UserName ?? "user";
        var userEmail = _currentUser.UserEmail ?? string.Empty;
        var displayName = _currentUser.DisplayName ?? userName;

        // Count how many memos this user has already created today to calculate the sequence number
        // The sequence is per-user per-day (e.g., if jsmith has 2 memos today, the next is 003)
        var today = DateTime.UtcNow.Date;
        var existingCount = await _memoRepository.GetMemoCountByAuthorAndDateAsync(userId, today, cancellationToken);

        // The sequence starts at 1 and increments for each memo on the same day
        var sequenceNumber = existingCount + 1;

        // Generate the memo number using the domain service.
        // UserName is typically the full email address (e.g. "jerry.yoon@qcells.com").
        // Use only the local part (before '@') so the number reads "jerryyoon-20260302-001"
        // rather than including the domain. SanitiseUsername strips dots and other non-alnum chars.
        var localPart = userName.Contains('@') ? userName[..userName.IndexOf('@')] : userName;
        var memoNumber = MemoNumberGenerator.Generate(localPart, DateTime.UtcNow, sequenceNumber);

        _logger.LogInformation("Creating new memo '{MemoNumber}' for user {UserId}", memoNumber, userId);

        // Build the Memo entity from the command data
        var memo = new Memo
        {
            Id = Guid.NewGuid(),
            MemoNumber = memoNumber,
            Title = request.Title,
            Content = request.Content,
            AuthorId = userId,
            AuthorName = displayName,
            AuthorEmail = userEmail,
            DateCreated = DateTime.UtcNow,
            Tags = request.Tags,
            ToRecipients = request.ToRecipients,
            CcRecipients = request.CcRecipients,
            IsConfidential = request.IsConfidential,
            // New memos always start as Draft — the author must explicitly submit them
            Status = Domain.Enums.MemoStatus.Draft
        };

        // Create an ApprovalStep for each approver specified in the command
        // The Order field starts at 1 and increments (reserved for future sequential approval)
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
                // All steps start as Pending — the approver hasn't acted yet
                Decision = Domain.Enums.ApprovalDecision.Pending
            });
        }

        // Persist the new memo (and its approval steps via EF Core cascade) to the database
        var created = await _memoRepository.CreateAsync(memo, cancellationToken);

        _logger.LogInformation("Memo '{MemoNumber}' (ID: {MemoId}) created successfully", memoNumber, created.Id);

        // Convert the domain entity to a DTO for return to the caller
        return _mapper.Map<MemoDto>(created);
    }
}
