using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Handles the <see cref="GetMemoByIdQuery"/> by loading a memo from the database
/// and mapping it to a MemoDto.
/// </summary>
public class GetMemoByIdQueryHandler : IRequestHandler<GetMemoByIdQuery, MemoDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMemoByIdQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public GetMemoByIdQueryHandler(
        IMemoRepository memoRepository,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetMemoByIdQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>Handles the get memo by ID query.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if no memo with the given ID exists.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown when a non-recipient tries to open a confidential memo.</exception>
    public async Task<MemoDto> Handle(GetMemoByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading memo {MemoId}", request.MemoId);

        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Enforce confidential access: only author, recipients, approvers, and admins may open
        if (memo.IsConfidential)
        {
            var userId    = _currentUserService.UserId    ?? string.Empty;
            var userEmail = _currentUserService.UserEmail ?? string.Empty;

            var isAdmin     = _currentUserService.IsAdmin;
            var isAuthor    = memo.AuthorId == userId;
            var isRecipient = memo.ToRecipients.Any(r => string.Equals(r, userEmail, StringComparison.OrdinalIgnoreCase))
                           || memo.CcRecipients.Any(r => string.Equals(r, userEmail, StringComparison.OrdinalIgnoreCase));
            var isApprover  = memo.ApprovalSteps.Any(s => s.ApproverId == userId);

            if (!isAdmin && !isAuthor && !isRecipient && !isApprover)
                throw new UnauthorizedMemoAccessException(memo.Id, userId, "View");
        }

        return _mapper.Map<MemoDto>(memo);
    }
}
