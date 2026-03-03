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
    private readonly ILogger<GetMemoByIdQueryHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public GetMemoByIdQueryHandler(
        IMemoRepository memoRepository,
        IMapper mapper,
        ILogger<GetMemoByIdQueryHandler> logger)
    {
        _memoRepository = memoRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the get memo by ID query.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if no memo with the given ID exists.</exception>
    public async Task<MemoDto> Handle(GetMemoByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading memo {MemoId}", request.MemoId);

        // Retrieve the memo from the database (includes ApprovalSteps and Attachments)
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Map the domain entity to the DTO and return it
        return _mapper.Map<MemoDto>(memo);
    }
}
