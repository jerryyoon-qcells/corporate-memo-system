using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Query to retrieve all memos created by the currently authenticated user.
/// Returns memos of all statuses (Draft, PendingApproval, Approved, Rejected, Published).
/// Used by the "My Documents" dashboard tab.
/// Handled by <see cref="GetMyMemosQueryHandler"/>.
/// </summary>
public class GetMyMemosQuery : IRequest<List<MemoSummaryDto>>
{
    // No filter parameters needed — the handler uses the current user's ID automatically
}
