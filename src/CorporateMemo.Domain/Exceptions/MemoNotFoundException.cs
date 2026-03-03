namespace CorporateMemo.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested memo cannot be found in the data store.
/// This is a domain-level exception, meaning it represents a business rule violation:
/// you cannot operate on a memo that does not exist.
/// </summary>
public class MemoNotFoundException : Exception
{
    /// <summary>
    /// Gets the ID of the memo that was not found.
    /// </summary>
    public Guid MemoId { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MemoNotFoundException"/> with the memo ID.
    /// </summary>
    /// <param name="memoId">The ID of the memo that could not be found.</param>
    public MemoNotFoundException(Guid memoId)
        : base($"Memo with ID '{memoId}' was not found.")
    {
        // Store the ID so callers can inspect which memo was not found
        MemoId = memoId;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MemoNotFoundException"/> with the memo number.
    /// Used when looking up a memo by its human-readable number (e.g., "jsmith-20260302-001").
    /// </summary>
    /// <param name="memoNumber">The memo number that could not be found.</param>
    public MemoNotFoundException(string memoNumber)
        : base($"Memo with number '{memoNumber}' was not found.")
    {
        // MemoId is not known in this case — leave as default Guid
        MemoId = Guid.Empty;
    }
}
