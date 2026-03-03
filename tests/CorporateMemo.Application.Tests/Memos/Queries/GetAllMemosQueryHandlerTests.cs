using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Application.Mappings;
using CorporateMemo.Application.Memos.Queries;
using CorporateMemo.Application.Tests.Helpers;
using CorporateMemo.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorporateMemo.Application.Tests.Memos.Queries;

/// <summary>
/// Unit tests for <see cref="GetAllMemosQueryHandler"/>.
///
/// Scenarios covered:
/// - Happy path: returns mapped list of MemoSummaryDtos
/// - Empty result: repository returns empty list
/// - Filter parameters are passed through to the repository
/// </summary>
public class GetAllMemosQueryHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly IMapper _mapper;

    public GetAllMemosQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetAllMemosQueryHandler CreateHandler() =>
        new(_repoMock.Object, _mapper, NullLogger<GetAllMemosQueryHandler>.Instance);

    // ============================================================
    // Happy path
    // ============================================================

    /// <summary>
    /// When the repository returns memos, the handler should return a corresponding list of DTOs.
    /// </summary>
    [Fact]
    public async Task Handle_MemosExist_ReturnsMappedSummaryDtos()
    {
        // Arrange — repository returns two published memos
        var memos = new List<Domain.Entities.Memo>
        {
            TestMemoBuilder.Create().WithStatus(MemoStatus.Published).Build(),
            TestMemoBuilder.Create().WithStatus(MemoStatus.Published).Build()
        };

        _repoMock
            .Setup(r => r.GetAllAsync(
                It.IsAny<MemoStatus?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(memos);

        // Act
        var result = await CreateHandler().Handle(new GetAllMemosQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<MemoSummaryDto>();
    }

    /// <summary>
    /// When the repository returns an empty list, the handler should return an empty list.
    /// </summary>
    [Fact]
    public async Task Handle_NoMemosExist_ReturnsEmptyList()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetAllAsync(
                It.IsAny<MemoStatus?>(), It.IsAny<string?>(), It.IsAny<List<string>?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Memo>());

        // Act
        var result = await CreateHandler().Handle(new GetAllMemosQuery(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// The query's filter parameters should be forwarded to IMemoRepository.GetAllAsync unchanged.
    /// This ensures the handler acts as a transparent passthrough for filters.
    /// </summary>
    [Fact]
    public async Task Handle_WithFilters_PassesFiltersToRepository()
    {
        // Arrange
        var query = new GetAllMemosQuery
        {
            Status = MemoStatus.Draft,
            AuthorId = "specific-author",
            SearchTerm = "budget",
            DateFrom = new DateTime(2026, 1, 1),
            DateTo = new DateTime(2026, 12, 31),
            Tags = new List<string> { "finance" }
        };

        _repoMock
            .Setup(r => r.GetAllAsync(
                MemoStatus.Draft,           // status filter
                "specific-author",          // author filter
                It.Is<List<string>?>(t => t != null && t.Contains("finance")), // tags
                new DateTime(2026, 1, 1),   // dateFrom
                new DateTime(2026, 12, 31), // dateTo
                "budget",                   // search term
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Memo>());

        // Act
        await CreateHandler().Handle(query, CancellationToken.None);

        // Assert — verify the correct overload was called with the correct arguments
        _repoMock.Verify(r => r.GetAllAsync(
            MemoStatus.Draft,
            "specific-author",
            It.Is<List<string>?>(t => t != null && t.Contains("finance")),
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            "budget",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
