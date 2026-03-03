using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Handles the <see cref="GetMyMemosQuery"/> by retrieving all memos
/// created by the currently authenticated user.
/// </summary>
public class GetMyMemosQueryHandler : IRequestHandler<GetMyMemosQuery, List<MemoSummaryDto>>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<GetMyMemosQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public GetMyMemosQueryHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IMapper mapper,
        ILogger<GetMyMemosQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the get my memos query.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the user is not authenticated.</exception>
    public async Task<List<MemoSummaryDto>> Handle(GetMyMemosQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to view their memos.");

        _logger.LogDebug("Loading memos for user {UserId}", userId);

        // Get all memos authored by the current user (all statuses)
        var memos = await _memoRepository.GetByAuthorAsync(userId, cancellationToken);

        return _mapper.Map<List<MemoSummaryDto>>(memos);
    }
}
