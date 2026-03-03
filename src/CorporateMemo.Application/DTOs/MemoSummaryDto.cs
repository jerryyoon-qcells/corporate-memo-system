using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.DTOs;

/// <summary>
/// Lightweight Data Transfer Object for memo list views on the Dashboard.
/// Only contains the fields needed for list display (not the full content or related data).
/// Using this DTO instead of MemoDto avoids loading large content fields when only a summary is needed.
/// </summary>
public class MemoSummaryDto
{
    /// <summary>Gets or sets the unique identifier of the memo.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-readable memo number (e.g., "jsmith-20260302-001").</summary>
    public string MemoNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the title of the memo. Displayed as a clickable link in the list.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the author for sorting and display.</summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>Gets or sets the author's user ID (used for filtering by author).</summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the memo was created. Used for sorting and date filtering.</summary>
    public DateTime DateCreated { get; set; }

    /// <summary>Gets or sets the current lifecycle status. Displayed as a colored badge in the list.</summary>
    public MemoStatus Status { get; set; }

    /// <summary>Gets or sets the list of hashtag keywords. Displayed as comma-separated text in the list.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets whether this memo is marked as confidential.</summary>
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Gets a display-friendly string of all tags joined by commas.
    /// Example: "finance, Q1, budget"
    /// </summary>
    public string TagsDisplay => Tags.Any() ? string.Join(", ", Tags) : string.Empty;
}
