using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Application.Mappings;
using CorporateMemo.Application.Memos.Commands;
using CorporateMemo.Application.Tests.Helpers;
using CorporateMemo.Domain.Exceptions;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CorporateMemo.Application.Tests.Memos.Commands;

/// <summary>
/// Unit tests for <see cref="UploadAttachmentCommandHandler"/> and
/// <see cref="UploadAttachmentCommandValidator"/>.
///
/// Scenarios covered:
/// - C1: File too large → ValidationException (via validator)
/// - C1: Disallowed extension → ValidationException (via validator)
/// - C1: Zero-byte file → ValidationException (via validator)
/// - Happy path: valid file → attachment saved and AttachmentDto returned
/// - Authorization: non-author non-admin → UnauthorizedMemoAccessException
/// - Not found: memo does not exist → MemoNotFoundException
/// </summary>
public class UploadAttachmentCommandHandlerTests
{
    private readonly Mock<IMemoRepository> _repoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<IAttachmentStorage> _storageMock = new();
    private readonly IMapper _mapper;

    private const string AuthorId = "author-user-id";
    private const string OtherId  = "other-user-id";

    /// <summary>
    /// A simple test double for <see cref="IAttachmentSettings"/>.
    /// Uses 10 MB max size and allows only pdf/docx by default.
    /// </summary>
    private sealed class TestAttachmentSettings : IAttachmentSettings
    {
        public int MaxFileSizeMb { get; set; } = 10;
        public long MaxFileSizeBytes => (long)MaxFileSizeMb * 1024 * 1024;
        public IReadOnlyList<string> AllowedExtensions { get; set; } =
            new List<string> { "pdf", "docx", "png", "jpg" };
    }

    public UploadAttachmentCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MemoMappingProfile>());
        _mapper = config.CreateMapper();

        _currentUserMock.Setup(u => u.UserId).Returns(AuthorId);
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);

        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Domain.Entities.Memo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private UploadAttachmentCommandHandler CreateHandler() =>
        new(_repoMock.Object, _currentUserMock.Object, _storageMock.Object,
            _mapper, NullLogger<UploadAttachmentCommandHandler>.Instance);

    private static UploadAttachmentCommandValidator CreateValidator(
        IAttachmentSettings? settings = null) =>
        new(settings ?? new TestAttachmentSettings());

    /// <summary>Creates a valid command with the given file name and size.</summary>
    private static UploadAttachmentCommand MakeCommand(
        Guid memoId,
        string fileName = "document.pdf",
        long sizeBytes = 1024,
        string contentType = "application/pdf")
        => new()
        {
            MemoId      = memoId,
            FileName    = fileName,
            ContentType = contentType,
            FileStream  = new MemoryStream(new byte[sizeBytes]),
            FileSizeBytes = sizeBytes
        };

    // ── C1: Validation — file too large ────────────────────────────────────

    /// <summary>
    /// A file whose size exceeds <c>MaxFileSizeBytes</c> must be rejected by the
    /// validator before the handler runs.
    /// C1 fix: previously no size check was performed anywhere.
    /// </summary>
    [Fact]
    public async Task Validate_FileTooLarge_ThrowsValidationException()
    {
        // Arrange
        var settings = new TestAttachmentSettings { MaxFileSizeMb = 1 }; // 1 MB
        var validator = CreateValidator(settings);

        var command = MakeCommand(
            memoId: Guid.NewGuid(),
            fileName: "large-file.pdf",
            sizeBytes: 2 * 1024 * 1024); // 2 MB — exceeds 1 MB limit

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert — validation must fail with the size error message
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("maximum allowed size"));
    }

    // ── C1: Validation — disallowed extension ─────────────────────────────

    /// <summary>
    /// A file with an extension not in the allowed list must be rejected.
    /// C1 fix: previously no extension check was performed anywhere.
    /// </summary>
    [Fact]
    public async Task Validate_DisallowedExtension_ThrowsValidationException()
    {
        // Arrange
        var validator = CreateValidator(); // default settings allow pdf, docx, png, jpg

        var command = MakeCommand(
            memoId: Guid.NewGuid(),
            fileName: "malicious.exe", // .exe is not in the allowlist
            sizeBytes: 512,
            contentType: "application/octet-stream");

        // Act
        var result = await validator.ValidateAsync(command);

        // Assert — validation must fail with the extension error message
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("not allowed"));
    }

    /// <summary>
    /// A file with a shell script extension must be rejected.
    /// </summary>
    [Fact]
    public async Task Validate_ShellScriptExtension_ThrowsValidationException()
    {
        var validator = CreateValidator();
        var command = MakeCommand(
            memoId: Guid.NewGuid(),
            fileName: "backdoor.sh",
            sizeBytes: 128,
            contentType: "text/plain");

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not allowed"));
    }

    // ── C1: Validation — zero-byte file ───────────────────────────────────

    /// <summary>
    /// A file with zero bytes must be rejected (FileSizeBytes must be > 0).
    /// </summary>
    [Fact]
    public async Task Validate_ZeroByteFile_ThrowsValidationException()
    {
        var validator = CreateValidator();
        var command = MakeCommand(
            memoId: Guid.NewGuid(),
            fileName: "empty.pdf",
            sizeBytes: 0);

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains("must not be empty"));
    }

    // ── C1: Validation — valid file passes ────────────────────────────────

    /// <summary>
    /// A file with a valid extension and size within the limit must pass validation.
    /// </summary>
    [Fact]
    public async Task Validate_ValidFile_PassesValidation()
    {
        var validator = CreateValidator();
        var command = MakeCommand(
            memoId: Guid.NewGuid(),
            fileName: "report.pdf",
            sizeBytes: 512 * 1024); // 512 KB — within 10 MB limit

        var result = await validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    // ── Handler — happy path ──────────────────────────────────────────────

    /// <summary>
    /// When a valid file is uploaded by the memo author, the attachment should be
    /// saved and the returned DTO should reflect the uploaded file's metadata.
    /// </summary>
    [Fact]
    public async Task Handle_ValidFile_AttachmentSavedAndDtoReturned()
    {
        // Arrange
        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .Build();

        const string storedPath = "abc123_document.pdf";
        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);
        _storageMock
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), "document.pdf", "application/pdf", It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedPath);

        var command = MakeCommand(memo.Id, "document.pdf", 1024, "application/pdf");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — returned DTO should carry the correct file metadata
        result.Should().NotBeNull();
        result.FileName.Should().Be("document.pdf");
        result.ContentType.Should().Be("application/pdf");
        result.FileSizeBytes.Should().Be(1024);
        result.StoragePath.Should().Be(storedPath);
        result.MemoId.Should().Be(memo.Id);

        // Storage.SaveAsync must have been called exactly once
        _storageMock.Verify(
            s => s.SaveAsync(It.IsAny<Stream>(), "document.pdf", "application/pdf", It.IsAny<CancellationToken>()),
            Times.Once);

        // Memo must be updated to persist the new attachment record
        _repoMock.Verify(
            r => r.UpdateAsync(memo, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Handler — memo not found ──────────────────────────────────────────

    /// <summary>
    /// When the memo does not exist, MemoNotFoundException must be thrown
    /// and no file should be written to storage.
    /// </summary>
    [Fact]
    public async Task Handle_MemoNotFound_ThrowsMemoNotFoundException()
    {
        // Arrange
        var memoId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(memoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Memo?)null);

        var command = MakeCommand(memoId);

        // Act & Assert
        var act = () => CreateHandler().Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<MemoNotFoundException>();

        // Storage must not be called for a non-existent memo
        _storageMock.Verify(
            s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Handler — unauthorized non-author ─────────────────────────────────

    /// <summary>
    /// When a non-author non-admin user attempts to upload an attachment,
    /// UnauthorizedMemoAccessException must be thrown.
    /// </summary>
    [Fact]
    public async Task Handle_NonAuthorNonAdmin_ThrowsUnauthorizedMemoAccessException()
    {
        // Arrange — current user is not the author
        _currentUserMock.Setup(u => u.UserId).Returns(OtherId);
        _currentUserMock.Setup(u => u.IsAdmin).Returns(false);

        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId) // different from OtherId
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);

        var command = MakeCommand(memo.Id);

        // Act & Assert
        var act = () => CreateHandler().Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedMemoAccessException>();
    }

    /// <summary>
    /// An admin user must be able to upload attachments to any memo.
    /// </summary>
    [Fact]
    public async Task Handle_AdminUser_CanUploadToAnyMemo()
    {
        // Arrange — current user is admin, not the author
        _currentUserMock.Setup(u => u.UserId).Returns(OtherId);
        _currentUserMock.Setup(u => u.IsAdmin).Returns(true);

        var memo = TestMemoBuilder.Create()
            .WithAuthorId(AuthorId)
            .Build();

        _repoMock.Setup(r => r.GetByIdAsync(memo.Id, It.IsAny<CancellationToken>())).ReturnsAsync(memo);
        _storageMock
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored_file.pdf");

        var command = MakeCommand(memo.Id);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — upload should succeed for admin
        result.Should().NotBeNull();
    }
}
