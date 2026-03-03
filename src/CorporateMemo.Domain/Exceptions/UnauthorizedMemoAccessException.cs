namespace CorporateMemo.Domain.Exceptions;

/// <summary>
/// Exception thrown when a user attempts an action on a memo they are not authorised to perform.
/// Examples: editing another user's memo, approving a memo you are not assigned to.
/// </summary>
public class UnauthorizedMemoAccessException : Exception
{
    /// <summary>
    /// Gets the ID of the memo the user tried to access.
    /// </summary>
    public Guid MemoId { get; }

    /// <summary>
    /// Gets the ID of the user who attempted the unauthorised action.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Gets the action the user attempted to perform.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="UnauthorizedMemoAccessException"/>.
    /// </summary>
    /// <param name="memoId">The ID of the memo being accessed.</param>
    /// <param name="userId">The ID of the user attempting the action.</param>
    /// <param name="action">A short description of the action attempted (e.g., "Edit", "Delete").</param>
    public UnauthorizedMemoAccessException(Guid memoId, string userId, string action)
        : base($"User '{userId}' is not authorised to perform action '{action}' on memo '{memoId}'.")
    {
        MemoId = memoId;
        UserId = userId;
        Action = action;
    }
}
