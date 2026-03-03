using CorporateMemo.Application.DTOs;
using CorporateMemo.Domain.Enums;
using MediatR;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Query for the advanced search feature on the Dashboard.
/// Supports all filter parameters from the advanced search panel.
/// Handled by <see cref="SearchMemosQueryHandler"/>.
/// </summary>
public class SearchMemosQuery : IRequest<List<MemoSummaryDto>>
{
    /// <summary>Optional: keyword search matching title, content, and tags (case-insensitive).</summary>
    public string? SearchTerm { get; set; }

    /// <summary>Optional: filter by one or more memo statuses.</summary>
    public List<MemoStatus>? Statuses { get; set; }

    /// <summary>Optional: filter by author user ID.</summary>
    public string? AuthorId { get; set; }

    /// <summary>Optional: filter by tag keyword.</summary>
    public string? Tag { get; set; }

    /// <summary>Optional: only return memos created on or after this date.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Optional: only return memos created on or before this date.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Optional: filter by whether memos are confidential (null = all, true = confidential only, false = non-confidential only).</summary>
    public bool? IsConfidential { get; set; }

    /// <summary>Optional: filter by approver user ID (memos where this user is an assigned approver).</summary>
    public string? ApproverId { get; set; }
}
