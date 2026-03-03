using CorporateMemo.Application.DTOs;
using MediatR;

namespace CorporateMemo.Application.Memos.Queries;

/// <summary>
/// Query to retrieve all memos pending the current user's approval.
/// Returns only memos in PendingApproval status where the current user is an assigned approver.
/// Used by the "My Approvals" dashboard tab.
/// Handled by <see cref="GetMyApprovalsQueryHandler"/>.
/// </summary>
public class GetMyApprovalsQuery : IRequest<List<MemoSummaryDto>>
{
    // No filter parameters needed — the handler uses the current user's ID automatically
}
