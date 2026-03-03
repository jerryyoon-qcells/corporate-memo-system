using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Handles the <see cref="GetMyApprovalsQuery"/> by retrieving all memos
/// that the current user is assigned to approve and that are still pending.
/// </summary>
public class GetMyApprovalsQueryHandler : IRequestHandler<GetMyApprovalsQuery, List<MemoSummaryDto>>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<GetMyApprovalsQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public GetMyApprovalsQueryHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<GetMyApprovalsQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the get my approvals query.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the user is not authenticated.</exception>
    public async Task<List<MemoSummaryDto>> Handle(GetMyApprovalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to view their approvals.");

        _logger.LogDebug("Loading pending approvals for user {UserId}", userId);

        // Get all memos in PendingApproval status where this user is an assigned approver
        var memos = await _memoRepository.GetPendingApprovalsForUserAsync(userId, cancellationToken);

        return _mapper.Map<List<MemoSummaryDto>>(memos);
    }
}
