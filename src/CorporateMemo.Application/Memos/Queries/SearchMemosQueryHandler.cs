using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Handles the <see cref="SearchMemosQuery"/> by applying all specified filters
/// to retrieve a matching list of memos.
/// </summary>
public class SearchMemosQueryHandler : IRequestHandler<SearchMemosQuery, List<MemoSummaryDto>>
{
    private readonly IMemoRepository _memoRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchMemosQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public SearchMemosQueryHandler(
        IMemoRepository memoRepository,
        IMapper mapper,
        ILogger<SearchMemosQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the advanced search query.</summary>
    public async Task<List<MemoSummaryDto>> Handle(SearchMemosQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing advanced search: Term={Term}, Author={Author}, Statuses={Statuses}",
            request.SearchTerm, request.AuthorId, request.Statuses?.Count);

        // For the advanced search, we use the GetAllAsync method with the first status filter
        // If multiple statuses are requested, we get all memos and filter in memory
        // (For a production system, we would add multi-status filtering to the repository)
        var tags = request.Tag != null ? new List<string> { request.Tag } : null;

        // Get all memos matching the common filters (status, author, tags, dates, searchterm)
        // We pass null for status if multiple statuses are requested
        MemoStatus? singleStatus = request.Statuses?.Count == 1 ? request.Statuses[0] : null;

        var memos = await _memoRepository.GetAllAsync(
            status: singleStatus,
            author: request.AuthorId,
            tags: tags,
            dateFrom: request.DateFrom,
            dateTo: request.DateTo,
            searchTerm: request.SearchTerm,
            ct: cancellationToken);

        // Apply additional filters that the repository doesn't handle directly

        // Filter by multiple statuses (when more than one is selected in advanced search)
        if (request.Statuses != null && request.Statuses.Count > 1)
        {
            memos = memos.Where(m => request.Statuses.Contains(m.Status)).ToList();
        }

        // Filter by confidential flag
        if (request.IsConfidential.HasValue)
        {
            memos = memos.Where(m => m.IsConfidential == request.IsConfidential.Value).ToList();
        }

        // Filter by approver (memos where a specific user is an assigned approver)
        if (!string.IsNullOrEmpty(request.ApproverId))
        {
            memos = memos.Where(m =>
                m.ApprovalSteps.Any(s => s.ApproverId == request.ApproverId)).ToList();
        }

        return _mapper.Map<List<MemoSummaryDto>>(memos);
    }
}
