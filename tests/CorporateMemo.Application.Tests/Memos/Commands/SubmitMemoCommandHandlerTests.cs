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
/// Unit tests for <see cref="SubmitMemoCommandHandler"/>.
///
/// Scenarios covered:
/// - Happy path: submit with approvers → PendingApproval
/// - Happy path: submit without approvers → Published (auto-publish)
/// - Not found: memo does not exist
/// - Unauthorized: user is not the author
/// - Invalid state: memo is not Draft or Rejected
/// - Invalid state: memo has no To recipients
/// </summary>
public class SubmitMemoCommandHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IEmailService> _emailMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly IMapper _mapper;

    private const string AuthorId = "author-user-id";

    public SubmitMemoCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();

        _currentUserMock.Setup(u => u.UserId).Returns(AuthorId);
        _currentUserMock.Setup(u => u.DisplayName).Returns("J Smith");

        // Default: UpdateAsync is a no-op
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private SubmitMemoCommandHandler CreateHandler() =>
        new(_repoMock.Object, _currentUserMock.Object, _emailMock.Object,
            _notificationMock.Object, _mapper, NullLogger<SubmitMemoCommandHandler>.Instance);

    private SubmitMemoCommand MakeCommand(Guid memoId) =>
        new() { MemoId = memoId, BaseUrl = "https://localhost" };

    // ============================================================
    // Happy path — with approvers → PendingApproval
    // ============================================================

    [Fact]
    public async Task Handle_DraftMemoWithApprovers_TransitionsToPendingApproval()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.Draft)
            .WithToRecipients("team@example.com")
            .WithApprover("approver-1")
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        result.Status.Should().Be(MemoStatus.PendingApproval);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ============================================================
    // Happy path — without approvers → Published (auto-publish)
    // ============================================================

    [Fact]
    public async Task Handle_DraftMemoWithNoApprovers_AutoPublishes()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.Draft)
            .WithToRecipients("team@example.com")
            // No approvers → auto-publish path
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        result.Status.Should().Be(MemoStatus.Published);
    }

    /// <summary>
    /// A Rejected memo with no approvers should also auto-publish successfully when re-submitted.
    /// </summary>
    [Fact]
    public async Task Handle_RejectedMemoWithNoApprovers_AutoPublishes()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.Rejected)
            .WithToRecipients("team@example.com")
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert
        result.Status.Should().Be(MemoStatus.Published);
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
    // Unauthorized
    // ============================================================

    [Fact]
    public async Task Handle_DifferentUserSubmitting_ThrowsUnauthorizedMemoAccessException()
    {
        // Arrange — memo belongs to a different author
        var memo = TestMemoBuilder.Create()
            .WithAuthorId("different-author-id")
            .WithStatus(MemoStatus.Draft)
            .WithToRecipients("team@example.com")
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedMemoAccessException>();
    }

    // ============================================================
    // Invalid state — wrong status
    // ============================================================

    [Theory]
    [InlineData(MemoStatus.PendingApproval)]
    [InlineData(MemoStatus.Approved)]
    [InlineData(MemoStatus.Published)]
    public async Task Handle_MemoInNonSubmittableStatus_ThrowsInvalidMemoStateException(MemoStatus status)
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(status)
            .WithToRecipients("team@example.com")
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidMemoStateException>();
    }

    // ============================================================
    // Invalid state — no recipients
    // ============================================================

    [Fact]
    public async Task Handle_MemoWithNoRecipients_ThrowsInvalidMemoStateException()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.Draft)
            .WithNoRecipients() // No To recipients
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Act & Assert
        var act = () => CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidMemoStateException>();
    }

    // ============================================================
    // Email failure does not roll back
    // ============================================================

    /// <summary>
    /// If sending the approval email fails, the memo status should still be PendingApproval.
    /// Email failures must not roll back the primary operation (per spec).
    /// </summary>
    [Fact]
    public async Task Handle_EmailSendFails_StillTransitionsToPendingApproval()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .WithStatus(MemoStatus.Draft)
            .WithToRecipients("team@example.com")
            .WithApprover("approver-1")
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        // Email service throws — simulates SMTP down
        _emailMock
            .Setup(e => e.SendApprovalRequestAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP server unavailable"));

        // Act — should NOT throw despite email failure
        var result = await CreateHandler().Handle(MakeCommand(memo.Id), CancellationToken.None);

        // Assert — the memo was still updated
        result.Status.Should().Be(MemoStatus.PendingApproval);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
