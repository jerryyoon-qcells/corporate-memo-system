using CorporateMemo.Application.DTOs;
using CorporateMemo.Domain.Enums;
using MediatR;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Query to retrieve all memos with optional filtering.
/// Used by the "All Documents" dashboard tab (filtered to Published status by default).
/// Handled by <see cref="GetAllMemosQueryHandler"/>.
/// </summary>
public class GetAllMemosQuery : IRequest<List<MemoSummaryDto>>
{
    /// <summary>Optional: filter by memo status. Defaults to Published for the All Documents tab.</summary>
    public MemoStatus? Status { get; set; } = MemoStatus.Published;

    /// <summary>Optional: filter by author user ID.</summary>
    public string? AuthorId { get; set; }

    /// <summary>Optional: filter to memos containing any of these tags.</summary>
    public List<string>? Tags { get; set; }

    /// <summary>Optional: only return memos created on or after this date.</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Optional: only return memos created on or before this date.</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Optional: case-insensitive search term matching title, content, and tags.</summary>
    public string? SearchTerm { get; set; }
}
