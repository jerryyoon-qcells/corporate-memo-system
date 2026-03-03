using AutoMapper;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Application.Mappings;
using CorporateMemo.Application.Memos.Queries;
using CorporateMemo.Application.Tests.Helpers;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorporateMemo.Application.Tests.Memos.Queries;

/// <summary>
/// Unit tests for <see cref="GetMemoByIdQueryHandler"/>.
///
/// Scenarios covered:
/// - Happy path: memo found → returns populated MemoDto
/// - Not found: memo does not exist → throws MemoNotFoundException
/// </summary>
public class GetMemoByIdQueryHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly IMapper _mapper;

    public GetMemoByIdQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetMemoByIdQueryHandler CreateHandler() =>
        new(_repoMock.Object, _mapper, NullLogger<GetMemoByIdQueryHandler>.Instance);

    // ============================================================
    // Happy path
    // ============================================================

    /// <summary>
    /// When a memo exists, the handler should return a MemoDto with all fields mapped.
    /// </summary>
    [Fact]
    public async Task Handle_MemoExists_ReturnsMemoDto()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithStatus(MemoStatus.Published)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(new GetMemoByIdQuery { MemoId = memo.Id }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(memo.Id);
        result.Title.Should().Be(memo.Title);
        result.Status.Should().Be(MemoStatus.Published);
    }

    /// <summary>
    /// The returned DTO should include the approval steps mapped from the entity.
    /// </summary>
    [Fact]
    public async Task Handle_MemoWithApprovalSteps_MappsApprovalSteps()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover("approver-1", "Alice", "alice@example.com", ApprovalDecision.Approved)
            .WithApprover("approver-2", "Bob", "bob@example.com", ApprovalDecision.Pending)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(new GetMemoByIdQuery { MemoId = memo.Id }, CancellationToken.None);

        // Assert
        result.ApprovalSteps.Should().HaveCount(2);
        result.ApprovalSteps.Should().Contain(s => s.ApproverId == "approver-1" && s.Decision == ApprovalDecision.Approved);
        result.ApprovalSteps.Should().Contain(s => s.ApproverId == "approver-2" && s.Decision == ApprovalDecision.Pending);
    }

    // ============================================================
    // Not found
    // ============================================================

    /// <summary>
    /// When the memo does not exist in the repository, a MemoNotFoundException should be thrown.
    /// </summary>
    [Fact]
    public async Task Handle_MemoNotFound_ThrowsMemoNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Memo?)null);

        // Act & Assert
        var act = () => CreateHandler().Handle(new GetMemoByIdQuery { MemoId = nonExistentId }, CancellationToken.None);
        await act.Should().ThrowAsync<MemoNotFoundException>();
    }

    /// <summary>
    /// The MemoNotFoundException should contain the requested memo ID.
    /// </summary>
    [Fact]
    public async Task Handle_MemoNotFound_ExceptionContainsMemoId()
    {
        // Arrange
        var memoId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(memoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Memo?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<MemoNotFoundException>(
            () => CreateHandler().Handle(new GetMemoByIdQuery { MemoId = memoId }, CancellationToken.None));

        exception.MemoId.Should().Be(memoId);
    }
}
