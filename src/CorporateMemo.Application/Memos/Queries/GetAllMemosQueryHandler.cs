using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Handles the <see cref="GetAllMemosQuery"/> by retrieving filtered memos from the database.
/// </summary>
public class GetAllMemosQueryHandler : IRequestHandler<GetAllMemosQuery, List<MemoSummaryDto>>
{
    private readonly IMemoRepository _memoRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllMemosQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public GetAllMemosQueryHandler(
        IMemoRepository memoRepository,
        IMapper mapper,
        ILogger<GetAllMemosQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the get all memos query with optional filters.</summary>
    public async Task<List<MemoSummaryDto>> Handle(GetAllMemosQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving memos with filters: Status={Status}, Author={Author}, Search={Search}",
            request.Status, request.AuthorId, request.SearchTerm);

        // Retrieve memos from the repository with all the filter parameters
        var memos = await _memoRepository.GetAllAsync(
            status: request.Status,
            author: request.AuthorId,
            tags: request.Tags,
            dateFrom: request.DateFrom,
            dateTo: request.DateTo,
            searchTerm: request.SearchTerm,
            ct: cancellationToken);

        // Map the domain entities to summary DTOs (lighter-weight, no content field)
        return _mapper.Map<List<MemoSummaryDto>>(memos);
    }
}
