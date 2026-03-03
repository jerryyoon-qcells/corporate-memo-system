using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Domain.Enums;
using CorporateMemo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of the <see cref="IMemoRepository"/> interface.
/// All data access for Memos, ApprovalSteps, and Attachments goes through this class.
///
/// Key design decisions:
/// - Always eager-load ApprovalSteps and Attachments with Include() to avoid N+1 query problems
/// - Use AsNoTracking() for read-only queries to improve performance
/// - All methods are async to avoid blocking threads during database I/O
/// </summary>
public class MemoRepository : IMemoRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MemoRepository> _logger;

    /// <summary>
    /// Initializes the repository with the database context.
    /// The context is injected per-request (scoped lifetime) by the DI container.
    /// </summary>
    public MemoRepository(ApplicationDbContext context, ILogger<MemoRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Returns a base query that always includes ApprovalSteps and Attachments.
    /// Using this helper avoids repeating Include() calls in every method.
    /// </summary>
    private IQueryable<Memo> GetBaseQuery()
    {
        return _context.Memos
            .Include(m => m.ApprovalSteps)  // Load all approval steps for this memo
            .Include(m => m.Attachments);   // Load all attachments for this memo
    }

    /// <inheritdoc/>
    public async Task<Memo?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // FirstOrDefaultAsync returns null if not found (safer than Single which throws)
        // AsNoTracking would be good here for reads, but we need tracking for updates
        return await GetBaseQuery()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    /// <inheritdoc/>
    public async Task<List<Memo>> GetAllAsync(
        MemoStatus? status = null,
        string? author = null,
        List<string>? tags = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null,
        CancellationToken ct = default)
    {
        // Start with the base query (includes related data)
        // AsNoTracking improves performance for read-only queries (EF won't track changes)
        var query = GetBaseQuery().AsNoTracking();

        // Apply each filter only if it was provided (null means "no filter")
        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        if (!string.IsNullOrEmpty(author))
            query = query.Where(m => m.AuthorId == author);

        if (dateFrom.HasValue)
            query = query.Where(m => m.DateCreated >= dateFrom.Value);

        if (dateTo.HasValue)
            // Add 1 day to include the entire "to" date (e.g., "2026-03-02" includes all memos that day)
            query = query.Where(m => m.DateCreated < dateTo.Value.AddDays(1));

        // Case-insensitive search across Title, Content, and Tags (JSON column as string)
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearch = searchTerm.ToLower();
            query = query.Where(m =>
                m.Title.ToLower().Contains(lowerSearch) ||
                m.Content.ToLower().Contains(lowerSearch) ||
                // Search the JSON-serialized tags string (not ideal but works for MVP)
                m.Tags.Any(t => t.ToLower().Contains(lowerSearch)));
        }

        // Sort by DateCreated descending (newest first) for consistent ordering
        query = query.OrderByDescending(m => m.DateCreated);

        return await query.ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<Memo>> GetByAuthorAsync(string authorId, CancellationToken ct = default)
    {
        return await GetBaseQuery()
            .AsNoTracking()
            .Where(m => m.AuthorId == authorId)
            .OrderByDescending(m => m.DateCreated)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<List<Memo>> GetPendingApprovalsForUserAsync(string approverId, CancellationToken ct = default)
    {
        // Find all memos where:
        // 1. Status is PendingApproval AND
        // 2. The specified user is in the ApprovalSteps collection
        return await GetBaseQuery()
            .AsNoTracking()
            .Where(m =>
                m.Status == MemoStatus.PendingApproval &&
                m.ApprovalSteps.Any(s => s.ApproverId == approverId))
            .OrderByDescending(m => m.DateCreated)
            .ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetMemoCountByAuthorAndDateAsync(string authorId, DateTime date, CancellationToken ct = default)
    {
        // Count memos created by this author on the specified day (UTC)
        // This is used to calculate the sequence number for the next memo number
        var dayStart = date.Date;                    // Beginning of the day (midnight UTC)
        var dayEnd = date.Date.AddDays(1);           // Beginning of the next day

        return await _context.Memos
            .CountAsync(m =>
                m.AuthorId == authorId &&
                m.DateCreated >= dayStart &&
                m.DateCreated < dayEnd,
                ct);
    }

    /// <inheritdoc/>
    public async Task<Memo> CreateAsync(Memo memo, CancellationToken ct = default)
    {
        // Add the entity to the context's change tracker
        _context.Memos.Add(memo);

        // SaveChangesAsync executes the INSERT SQL statement
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Created memo {MemoId} with number {MemoNumber}", memo.Id, memo.MemoNumber);

        return memo;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Memo memo, CancellationToken ct = default)
    {
        // EF Core tracks the entity's changes automatically when it's loaded via GetByIdAsync
        // Calling Update explicitly handles the case where the entity may not be tracked
        _context.Memos.Update(memo);

        // SaveChangesAsync executes all pending UPDATE SQL statements
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Updated memo {MemoId}", memo.Id);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Memo memo, CancellationToken ct = default)
    {
        // Remove the entity from the context's change tracker
        _context.Memos.Remove(memo);

        // SaveChangesAsync executes the DELETE SQL statement
        // Cascading deletes configured in OnModelCreating will also remove related records
        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("Deleted memo {MemoId}", memo.Id);
    }
}
