using AutoMapper;
using CorporateMemo.Application.DTOs;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// Handles the <see cref="UploadAttachmentCommand"/> by saving the file to storage
/// and creating an Attachment database record linked to the memo.
///
/// Business rules enforced:
/// - Memo must exist
/// - Current user must be the memo author or an admin
/// - File is saved to the configured storage location
/// </summary>
public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private readonly IMemoRepository _memoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAttachmentStorage _attachmentStorage;
    private readonly IMapper _mapper;
    private readonly ILogger<UploadAttachmentCommandHandler> _logger;

    /// <summary>Initializes the handler with all required dependencies.</summary>
    public UploadAttachmentCommandHandler(
        IMemoRepository memoRepository,
        ICurrentUserService currentUser,
        IAttachmentStorage attachmentStorage,
        IMapper mapper,
        ILogger<UploadAttachmentCommandHandler> logger)
    {
        _memoRepository = memoRepository;
        _currentUser = currentUser;
        _attachmentStorage = attachmentStorage;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>Handles the upload attachment command.</summary>
    /// <exception cref="MemoNotFoundException">Thrown if the memo does not exist.</exception>
    /// <exception cref="UnauthorizedMemoAccessException">Thrown if the user is not the author.</exception>
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new InvalidOperationException("User must be authenticated to upload attachments.");

        // Load the memo from the database
        var memo = await _memoRepository.GetByIdAsync(request.MemoId, cancellationToken)
            ?? throw new MemoNotFoundException(request.MemoId);

        // Only the author (or admin) can upload attachments
        if (memo.AuthorId != userId && !_currentUser.IsAdmin)
            throw new UnauthorizedMemoAccessException(memo.Id, userId, "Upload Attachment");

        _logger.LogInformation("Uploading attachment '{FileName}' to memo {MemoId}", request.FileName, memo.Id);

        // Save the file to the storage system (local file system in MVP)
        // The storage service returns the path where the file is stored
        var storagePath = await _attachmentStorage.SaveAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        // Create the database record for this attachment
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            MemoId = memo.Id,
            FileName = request.FileName,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            StoragePath = storagePath,
            UploadedAt = DateTime.UtcNow
        };

        // Add the attachment to the memo's collection and update the database
        memo.Attachments.Add(attachment);
        await _memoRepository.UpdateAsync(memo, cancellationToken);

        _logger.LogInformation("Attachment {AttachmentId} uploaded successfully to memo {MemoId}",
            attachment.Id, memo.Id);

        return _mapper.Map<AttachmentDto>(attachment);
    }
}
