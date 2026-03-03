using CorporateMemo.Application.Interfaces;
using FluentValidation;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// FluentValidation validator for <see cref="UploadAttachmentCommand"/>.
///
/// C1 fix: This validator enforces the configured file-size limit and extension allowlist
/// that were previously defined in <c>AttachmentSettings</c> but never enforced anywhere.
/// It is automatically executed by the MediatR <c>ValidationBehaviour</c> pipeline before
/// the handler runs, so no file bytes are written to disk for invalid uploads.
/// </summary>
public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    /// <summary>
    /// Initializes all validation rules using the configured attachment settings.
    /// </summary>
    /// <param name="settings">
    /// Attachment settings injected from configuration — provides the allowed extensions
    /// list and the maximum file size. Injected as <see cref="IAttachmentSettings"/> so
    /// this Application-layer class does not depend on the Infrastructure layer.
    /// </param>
    public UploadAttachmentCommandValidator(IAttachmentSettings settings)
    {
        // Memo ID must be specified
        RuleFor(x => x.MemoId)
            .NotEmpty().WithMessage("A memo ID is required when uploading an attachment.");

        // File name must be provided and non-empty
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("A file name is required.");

        // Content type must be provided
        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("A content type is required.");

        // File must have a positive size (rejects zero-byte files)
        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("The uploaded file must not be empty.");

        // File size must not exceed the configured maximum
        RuleFor(x => x.FileSizeBytes)
            .LessThanOrEqualTo(settings.MaxFileSizeBytes)
            .WithMessage($"File size exceeds the maximum allowed size of {settings.MaxFileSizeMb} MB.");

        // File extension must be in the configured allowlist (case-insensitive)
        RuleFor(x => x.FileName)
            .Must(fileName => IsExtensionAllowed(fileName, settings.AllowedExtensions))
            .WithMessage(
                $"File type is not allowed. Permitted extensions: {string.Join(", ", settings.AllowedExtensions)}.");
    }

    /// <summary>
    /// Checks whether the file name's extension (without leading dot, lowercased)
    /// is present in the allowlist.
    /// </summary>
    private static bool IsExtensionAllowed(string fileName, IReadOnlyList<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Path.GetExtension returns ".pdf"; strip the leading dot and lowercase
        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        if (string.IsNullOrEmpty(ext))
            return false;

        return allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
}
