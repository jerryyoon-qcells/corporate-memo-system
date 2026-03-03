using FluentValidation;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// FluentValidation validator for the <see cref="UpdateMemoCommand"/>.
/// Ensures all updated fields meet the required constraints before the handler runs.
/// </summary>
public class UpdateMemoCommandValidator : AbstractValidator<UpdateMemoCommand>
{
    /// <summary>Initializes all validation rules for memo updates.</summary>
    public UpdateMemoCommandValidator()
    {
        // The memo ID must be a valid non-empty Guid
        RuleFor(x => x.MemoId)
            .NotEmpty().WithMessage("Memo ID is required for update.");

        // Title is required and has a maximum length of 100 characters
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Memo title is required.")
            .MaximumLength(100).WithMessage("Memo title cannot exceed 100 characters.");

        // Content is required and has a maximum length of 1000 characters per requirements.
        // M5 fix: Aligned with CreateMemoCommandValidator and the DB column max length of 1,000.
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Memo content is required.")
            .MaximumLength(1000).WithMessage("Memo content cannot exceed 1,000 characters.");

        // Tags list cannot exceed 20 items
        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 20)
            .WithMessage("A memo cannot have more than 20 tags.");

        // Each tag must not be empty
        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tags cannot be empty strings.")
            .MaximumLength(50).WithMessage("Each tag cannot exceed 50 characters.");

        // Each To recipient must be a valid email address
        RuleForEach(x => x.ToRecipients)
            .EmailAddress().WithMessage("Each To recipient must be a valid email address.");

        // Each CC recipient must be a valid email address
        RuleForEach(x => x.CcRecipients)
            .EmailAddress().WithMessage("Each CC recipient must be a valid email address.");
    }
}
