using CorporateMemo.Domain.Enums;

namespace CorporateMemo.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation is attempted on a memo that is in an invalid state for that operation.
/// For example: trying to delete a memo that has already been Published.
/// This enforces the memo lifecycle state machine.
/// </summary>
public class InvalidMemoStateException : Exception
{
    /// <summary>
    /// Gets the ID of the memo in the invalid state.
    /// </summary>
    public Guid MemoId { get; }

    /// <summary>
    /// Gets the current (invalid) status of the memo.
    /// </summary>
    public MemoStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the action that was attempted.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidMemoStateException"/>.
    /// </summary>
    /// <param name="memoId">The ID of the memo in the invalid state.</param>
    /// <param name="currentStatus">The memo's current status which prevents the action.</param>
    /// <param name="action">The action that was attempted (e.g., "Delete", "Submit").</param>
    public InvalidMemoStateException(Guid memoId, MemoStatus currentStatus, string action)
        : base($"Cannot perform '{action}' on memo '{memoId}' because its current status is '{currentStatus}'.")
    {
        MemoId = memoId;
        CurrentStatus = currentStatus;
        Action = action;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InvalidMemoStateException"/> with a custom message.
    /// Use this overload when you need to provide a more specific explanation.
    /// </summary>
    /// <param name="memoId">The ID of the memo in the invalid state.</param>
    /// <param name="currentStatus">The memo's current status.</param>
    /// <param name="action">The action that was attempted.</param>
    /// <param name="message">A custom error message explaining why the state transition is invalid.</param>
    public InvalidMemoStateException(Guid memoId, MemoStatus currentStatus, string action, string message)
        : base(message)
    {
        MemoId = memoId;
        CurrentStatus = currentStatus;
        Action = action;
    }
}
