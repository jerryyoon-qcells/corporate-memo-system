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
/// Unit tests for <see cref="RejectMemoCommandHandler"/>.
///
/// Scenarios covered:
/// - Happy path: single approver rejects → memo transitions to Rejected
/// - Not found: memo does not exist
/// - Invalid state: memo is not in PendingApproval status
/// - Unauthorized: user is not an assigned approver
/// - Invalid state: approver already voted (double-vote prevention)
/// </summary>
public class RejectMemoCommandHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly IMapper _mapper;

    private const string ApproverId = "approver-user-id";
    private const string AuthorId = "author-user-id";

    public RejectMemoCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();

        _currentUserMock.Setup(u => u.UserId).Returns(ApproverId);
        _currentUserMock.Setup(u => u.DisplayName).Returns("Bob Approver");

        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private RejectMemoCommandHandler CreateHandler() =>
        new(_repoMock.Object, _currentUserMock.Object, _emailMock.Object,
            _notificationMock.Object, _mapper, NullLogger<RejectMemoCommandHandler>.Instance);

    private RejectMemoCommand MakeCommand(Guid memoId, string? comment = "Needs revision") =>
        new() { MemoId = memoId, Comment = comment, BaseUrl = "https://localhost" };

    // ============================================================
    // Happy path
    // ============================================================

    /// <summary>
    /// An assigned approver rejecting a PendingApproval memo should transition it to Rejected.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRejection_TransitionsToRejected()
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
        result.Status.Should().Be(MemoStatus.Rejected);
    }

    /// <summary>
    /// The rejection comment should be stored on the approver's ApprovalStep.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRejection_StoresCommentOnApprovalStep()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        await CreateHandler().Handle(MakeCommand(memo.Id, "Please revise the budget section."), CancellationToken.None);

        // Assert — find the step for the current approver
        var step = memo.ApprovalSteps.First(s => s.ApproverId == ApproverId);
        step.Comment.Should().Be("Please revise the budget section.");
        step.Decision.Should().Be(ApprovalDecision.Rejected);
    }

    /// <summary>
    /// Even with two approvers, a single rejection immediately rejects the entire memo.
    /// </summary>
    [Fact]
    public async Task Handle_OneOfTwoApproversRejects_ImmediatelyRejectsMemo()
    {
        // Arrange — two approvers; current user rejects
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)               // rejects
            .WithApprover("second-approver-id")     // still pending
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        result.Status.Should().Be(MemoStatus.Rejected);
    }

    /// <summary>
    /// Rejection should call UpdateAsync exactly once to persist the state change.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRejection_PersistsMemoOnce()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()), Times.Once);
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
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover("different-approver-id")
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
        // Arrange — approver already voted (approved first, now trying to reject)
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.PendingApproval)
            .WithApprover(ApproverId, decision: ApprovalDecision.Approved)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidMemoStateException>()
            .WithMessage("*already*");
    }
}
