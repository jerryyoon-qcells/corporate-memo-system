using FluentValidation;

namespace CorporateMemo.Application.Memos.Commands;

/// <summary>
/// FluentValidation validator for the <see cref="CreateMemoCommand"/>.
/// Defines all validation rules that must pass before a memo can be created.
/// This is automatically run by the ValidationBehaviour pipeline before the handler executes.
/// </summary>
public class CreateMemoCommandValidator : AbstractValidator<CreateMemoCommand>
{
    /// <summary>
    /// Initializes all validation rules for memo creation.
    /// </summary>
    public CreateMemoCommandValidator()
    {
        // Title is required and has a maximum length of 100 characters
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Memo title is required.")
            .MaximumLength(100).WithMessage("Memo title cannot exceed 100 characters.");

        // Content is required and has a maximum length of 1000 characters per requirements.
        // M5 fix: The original validator used 10,000 characters, which is 10x the required limit.
        // The requirement specifies 1,000 characters of plain-text equivalent content.
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Memo content is required.")
            .MaximumLength(1000).WithMessage("Memo content cannot exceed 1,000 characters.");

        // Tags list cannot contain more than 20 items (per UX design limit)
        RuleFor(x => x.Tags)
            .Must(tags => tags.Count <= 20)
            .WithMessage("A memo cannot have more than 20 tags.");

        // Each tag must be non-empty and not too long
        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tags cannot be empty strings.")
            .MaximumLength(50).WithMessage("Each tag cannot exceed 50 characters.");

        // Each To recipient must be a valid email address format
        RuleForEach(x => x.ToRecipients)
            .EmailAddress().WithMessage("Each To recipient must be a valid email address.");

        // Each CC recipient must be a valid email address format
        RuleForEach(x => x.CcRecipients)
            .EmailAddress().WithMessage("Each CC recipient must be a valid email address.");

        // Validate each approver has required fields
        RuleForEach(x => x.Approvers)
            .ChildRules(approver =>
            {
                approver.RuleFor(a => a.UserId).NotEmpty().WithMessage("Approver user ID is required.");
                approver.RuleFor(a => a.Email).EmailAddress().WithMessage("Approver must have a valid email address.");
                approver.RuleFor(a => a.DisplayName).NotEmpty().WithMessage("Approver display name is required.");
            });
    }
}
