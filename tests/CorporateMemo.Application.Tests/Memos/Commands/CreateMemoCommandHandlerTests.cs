using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Application.Mappings;
using CorporateMemo.Application.Memos.Commands;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorporateMemo.Application.Tests.Memos.Commands;

/// <summary>
/// Unit tests for <see cref="CreateMemoCommandHandler"/>.
/// Tests verify: memo creation, memo number generation, approval step creation,
/// and authentication guard.
/// </summary>
public class CreateMemoCommandHandlerTests
{
    // ---- shared mocks ----
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly IMapper _mapper;

    // A fixed user ID used across test cases
    private const string AuthorId = "author-user-id";
    private const string AuthorName = "jsmith";
    private const string AuthorEmail = "jsmith@example.com";

    public CreateMemoCommandHandlerTests()
    {
        // Configure AutoMapper with the real mapping profile
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();

        // Default authenticated user setup — tests can override this individually
        _currentUserMock.Setup(u => u.UserId).Returns(AuthorId);
        _currentUserMock.Setup(u => u.UserName).Returns(AuthorName);
        _currentUserMock.Setup(u => u.UserEmail).Returns(AuthorEmail);
        _currentUserMock.Setup(u => u.DisplayName).Returns("J Smith");

        // Default: user has 0 memos today (so sequence number starts at 1)
        _repoMock
            .Setup(r => r.GetMemoCountByAuthorAndDateAsync(AuthorId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Default: CreateAsync returns whatever memo it receives
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Memo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Memo m, CancellationToken _) => m);
    }

    private CreateMemoCommandHandler CreateHandler() =>
        new(_repoMock.Object, _currentUserMock.Object, _mapper,
            NullLogger<CreateMemoCommandHandler>.Instance);

    // ============================================================
    // Happy path
    // ============================================================

    /// <summary>
    /// A valid command should create a memo with Draft status and return a MemoDto.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_ReturnsMemoDto()
    {
        // Arrange
        var command = new CreateMemoCommand
        {
            Title = "Q1 Budget Update",
            Content = "Please review the updated budget figures.",
            Tags = new List<string> { "finance", "q1" },
            ToRecipients = new List<string> { "team@example.com" }
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Q1 Budget Update");
        result.Status.Should().Be(MemoStatus.Draft);
        result.AuthorId.Should().Be(AuthorId);
    }

    /// <summary>
    /// Creating a memo should call IMemoRepository.CreateAsync exactly once.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_CallsCreateAsyncOnce()
    {
        // Arrange
        var command = new CreateMemoCommand { Title = "Test", Content = "Body" };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Memo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// The memo number should follow the [username]-[YYYYMMDD]-[seq] format
    /// and begin with the sanitised username.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_GeneratesMemoNumberWithCorrectFormat()
    {
        // Arrange
        var command = new CreateMemoCommand { Title = "Test", Content = "Body" };
        Memo? capturedMemo = null;
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Memo>(), It.IsAny<CancellationToken>()))
            .Callback<Memo, CancellationToken>((m, _) => capturedMemo = m)
            .ReturnsAsync((Memo m, CancellationToken _) => m);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — memo number format: jsmith-YYYYMMDD-001
        capturedMemo.Should().NotBeNull();
        capturedMemo!.MemoNumber.Should().StartWith("jsmith-");
        capturedMemo.MemoNumber.Should().EndWith("-001"); // First memo today (count = 0)
    }

    /// <summary>
    /// Sequence number should be existingCount + 1. If the user already has 3 memos today,
    /// the next memo should end with "-004".
    /// </summary>
    [Fact]
    public async Task Handle_UserHasExistingMemosToday_SequenceIncrements()
    {
        // Arrange — user already created 3 memos today
        _repoMock
            .Setup(r => r.GetMemoCountByAuthorAndDateAsync(AuthorId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new CreateMemoCommand { Title = "Test", Content = "Body" };
        Memo? capturedMemo = null;
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Memo>(), It.IsAny<CancellationToken>()))
            .Callback<Memo, CancellationToken>((m, _) => capturedMemo = m)
            .ReturnsAsync((Memo m, CancellationToken _) => m);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — sequence should be 4
        capturedMemo!.MemoNumber.Should().EndWith("-004");
    }

    /// <summary>
    /// Approvers specified in the command should be persisted as ApprovalStep entities
    /// on the new memo, all with Pending decision.
    /// </summary>
    [Fact]
    public async Task Handle_CommandWithApprovers_CreatesApprovalSteps()
    {
        // Arrange
        var command = new CreateMemoCommand
        {
            Title = "Test",
            Content = "Body",
            Approvers = new List<ApproverInfo>
            {
                new() { UserId = "approver-1", DisplayName = "Alice", Email = "alice@example.com" },
                new() { UserId = "approver-2", DisplayName = "Bob", Email = "bob@example.com" }
            }
        };

        Memo? capturedMemo = null;
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Memo>(), It.IsAny<CancellationToken>()))
            .Callback<Memo, CancellationToken>((m, _) => capturedMemo = m)
            .ReturnsAsync((Memo m, CancellationToken _) => m);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        capturedMemo!.ApprovalSteps.Should().HaveCount(2);
        capturedMemo.ApprovalSteps.All(s => s.Decision == ApprovalDecision.Pending).Should().BeTrue();
        capturedMemo.ApprovalSteps.Select(s => s.ApproverId)
            .Should().Contain(new[] { "approver-1", "approver-2" });
    }

    // ============================================================
    // Authentication guard
    // ============================================================

    /// <summary>
    /// If the current user is not authenticated (UserId is null), an
    /// InvalidOperationException should be thrown.
    /// </summary>
    [Fact]
    public async Task Handle_UnauthenticatedUser_ThrowsInvalidOperationException()
    {
        // Arrange — no user is authenticated
        _currentUserMock.Setup(u => u.UserId).Returns((string?)null);

        var command = new CreateMemoCommand { Title = "Test", Content = "Body" };

        // Act & Assert
        var act = () => CreateHandler().Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*authenticated*");
    }
}
