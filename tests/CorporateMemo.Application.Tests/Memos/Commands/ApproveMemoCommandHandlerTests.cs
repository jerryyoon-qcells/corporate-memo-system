using AutoMapper;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Application.Mappings;
using CorporateMemo.Application.Memos.Commands;
using CorporateMemo.Application.Tests.Helpers;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorporateMemo.Application.Tests.Memos.Commands;

/// <summary>
/// Unit tests for <see cref="ApproveMemoCommandHandler"/>.
///
/// Scenarios covered:
/// - Happy path: partial approval (still PendingApproval)
/// - Happy path: final approval (all approved → Published)
/// - Not found: memo does not exist
/// - Invalid state: memo is not in PendingApproval status
/// - Unauthorized: user is not an assigned approver
/// - Invalid state: approver already voted (double-vote prevention)
/// </summary>
public class ApproveMemoCommandHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly IMapper _mapper;

    private const string ApproverId = "approver-user-id";
    private const string AuthorId = "author-user-id";

    public ApproveMemoCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();

        _currentUserMock.Setup(u => u.UserId).Returns(ApproverId);
        _currentUserMock.Setup(u => u.DisplayName).Returns("Alice Approver");

        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ApproveMemoCommandHandler CreateHandler() =>
        new(_repoMock.Object, _currentUserMock.Object, _emailMock.Object,
            _notificationMock.Object, _mapper, NullLogger<ApproveMemoCommandHandler>.Instance);

    private ApproveMemoCommand MakeCommand(Guid memoId, string? comment = null) =>
        new() { MemoId = memoId, Comment = comment, BaseUrl = "https://localhost" };

    // ============================================================
    // Happy path — partial approval
    // ============================================================

    /// <summary>
    /// When one of two approvers approves, the memo should remain in PendingApproval.
    /// </summary>
    [Fact]
    public async Task Handle_OneOfTwoApproversApproves_RemainsInPendingApproval()
    {
        // Arrange — two approvers; only the current user approves
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)            // current user
            .WithApprover("second-approver-id") // second approver (still pending)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert — not all approved yet, so still PendingApproval
        result.Status.Should().Be(MemoStatus.PendingApproval);
    }

    // ============================================================
    // Happy path — final approval → Published
    // ============================================================

    /// <summary>
    /// When the last remaining approver approves, the memo should transition to Published.
    /// </summary>
    [Fact]
    public async Task Handle_LastApproverApproves_TransitionsToPublished()
    {
        // Arrange — only one approver; that approver is the current user
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id, "Looks good!"), CancellationToken.None);

        // Assert — all approvals received, memo published
        result.Status.Should().Be(MemoStatus.Published);
        result.ApprovalSteps.First(s => s.ApproverId == ApproverId).Decision
            .Should().Be(ApprovalDecision.Approved);
    }

    /// <summary>
    /// When the last approver approves, the ApprovedDate should be set.
    /// </summary>
    [Fact]
    public async Task Handle_LastApproverApproves_SetsApprovedDate()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        result.ApprovedDate.Should().NotBeNull();
        result.ApprovedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ============================================================
    // Not found
    // ============================================================

    [Fact]
    public async Task Handle_MemoNotFound_ThrowsMemoNotFoundException()
    {
        // Arrange
        var memoId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(memoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Memo?)null);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memoId), CancellationToken.None);
        await act.Should().ThrowAsync<MemoNotFoundException>();
    }

    // ============================================================
    // Invalid state — wrong status
    // ============================================================

    [Theory]
    [InlineData(MemoStatus.Draft)]
    [InlineData(MemoStatus.Approved)]
    [InlineData(MemoStatus.Rejected)]
    [InlineData(MemoStatus.Published)]
    public async Task Handle_MemoNotInPendingApproval_ThrowsInvalidMemoStateException(MemoStatus status)
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(status)
            .WithApprover(ApproverId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidMemoStateException>();
    }

    // ============================================================
    // Unauthorized — user is not an approver
    // ============================================================

    [Fact]
    public async Task Handle_UserNotAnApprover_ThrowsUnauthorizedMemoAccessException()
    {
        // Arrange — memo has a different approver, not the current user
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover("other-approver-id") // NOT the current user
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedMemoAccessException>();
    }

    // ============================================================
    // Invalid state — double-vote prevention
    // ============================================================

    [Fact]
    public async Task Handle_ApproverAlreadyDecided_ThrowsInvalidMemoStateException()
    {
        // Arrange — the approver already approved
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId, decision: ApprovalDecision.Approved) // already decided
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidMemoStateException>()
            .WithMessage("*already*");
    }
}
