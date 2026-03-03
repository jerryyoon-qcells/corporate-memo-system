using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Defines the contract for all memo-related data persistence operations.
/// This interface lives in the Application layer and is implemented in the Infrastructure layer.
/// Following Clean Architecture, the Application layer never references Infrastructure directly —
/// it only knows about this interface.
/// </summary>
public interface IMemoRepository
{
    /// <summary>
    /// Retrieves a single memo by its unique identifier, including all related data
    /// (ApprovalSteps and Attachments are eagerly loaded to avoid N+1 queries).
    /// </summary>
    /// <param name="id">The unique identifier of the memo to retrieve.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The matching Memo entity, or null if no memo with that ID exists.</returns>
    Task<Memo?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all memos from the data store, with optional filtering.
    /// This is used by the "All Documents" dashboard tab (filtered to Published status).
    /// </summary>
    /// <param name="status">Optional: filter by approval status (e.g., only Published memos).</param>
    /// <param name="author">Optional: filter by author's user ID.</param>
    /// <param name="tags">Optional: filter to memos that contain any of the specified tags.</param>
    /// <param name="dateFrom">Optional: only return memos created on or after this date.</param>
    /// <param name="dateTo">Optional: only return memos created on or before this date.</param>
    /// <param name="searchTerm">Optional: case-insensitive search across title, content, and tags.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>A list of memos matching all specified filters.</returns>
    Task<List<Memo>> GetAllAsync(
        MemoStatus? status = null,
        string? author = null,
        List<string>? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all memos created by a specific author, regardless of their status.
    /// This is used by the "My Documents" dashboard tab.
    /// </summary>
    /// <param name="authorId">The user ID of the author.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>All memos authored by the specified user.</returns>
    Task<List<Memo>> GetByAuthorAsync(string authorId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all memos that are awaiting approval from a specific user.
    /// This is used by the "My Approvals" dashboard tab.
    /// Only returns memos with PendingApproval status where the user is an assigned approver.
    /// </summary>
    /// <param name="approverId">The user ID of the approver.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>All memos pending approval from the specified user.</returns>
    Task<List<Memo>> GetPendingApprovalsForUserAsync(string approverId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of memos created by a specific user on a specific date.
    /// Used by MemoNumberGenerator to determine the next sequence number.
    /// </summary>
    /// <param name="authorId">The user ID of the author.</param>
    /// <param name="date">The date to count memos for.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The number of memos created by the author on that date.</returns>
    Task<int> GetMemoCountByAuthorAndDateAsync(string authorId, DateTime date, CancellationToken ct = default);

    /// <summary>
    /// Persists a new memo to the data store.
    /// The memo's Id and DateCreated should already be set before calling this method.
    /// </summary>
    /// <param name="memo">The new memo entity to save.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The saved memo with any database-generated values applied.</returns>
    Task<Memo> CreateAsync(Memo memo, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing memo in the data store.
    /// All changed fields are persisted, including nested collections (ApprovalSteps, Attachments).
    /// </summary>
    /// <param name="memo">The memo entity with updated values to save.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task UpdateAsync(Memo memo, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a memo from the data store.
    /// This method should only be called for Draft status memos per business rules.
    /// </summary>
    /// <param name="memo">The memo entity to delete.</param>
    /// <param name="ct">A cancellation token to cancel the async operation if needed.</param>
    Task DeleteAsync(Memo memo, CancellationToken ct = default);
}
