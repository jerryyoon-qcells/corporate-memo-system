using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.DTOs;

/// <summary>
/// Full Data Transfer Object for a memo, including all fields and related data.
/// Used on the Memo Detail page where all information needs to be displayed.
/// AutoMapper converts Memo entities to this DTO using the MemoMappingProfile.
/// </summary>
public class MemoDto
{
    /// <summary>Gets or sets the unique identifier of the memo.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable memo number (e.g., "jsmith-20260302-001").</summary>
    public string MemoNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the title of the memo. Maximum 100 characters.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique user ID of the author.</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the author.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the author.</summary>
    public string AuthorEmail { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the memo was created.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the memo was fully approved. Null if not yet approved.</summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>Gets or sets the body content of the memo (may contain HTML from rich text editor).</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the current lifecycle status of the memo.</summary>
    public MemoStatus Status { get; set; }

    /// <summary>Gets or sets whether this memo is marked as confidential.</summary>
    public bool IsConfidential { get; set; }

    /// <summary>Gets or sets the list of hashtag keywords.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the primary recipients (To field) as email addresses.</summary>
    public List<string> ToRecipients { get; set; } = new();

    /// <summary>Gets or sets the CC recipients as email addresses.</summary>
    public List<string> CcRecipients { get; set; } = new();

    /// <summary>Gets or sets the file attachments associated with this memo.</summary>
    public List<AttachmentDto> Attachments { get; set; } = new();

    /// <summary>Gets or sets the approval history for this memo.</summary>
    public List<ApprovalStepDto> ApprovalSteps { get; set; } = new();
}
