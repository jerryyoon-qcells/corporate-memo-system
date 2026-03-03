namespace CorporateMemo.Domain.Services;

/// <summary>
/// Provides memo number generation functionality following the corporate format.
/// The generated memo number is human-readable and encodes the author, date, and sequence.
///
/// Format: [username]-[YYYYMMDD]-[seq]
/// Example: jsmith-20260302-001
///
/// The sequence number is zero-padded to 3 digits and is scoped to the user + date combination.
/// This means "jsmith" can have 001, 002, 003 etc. on the same day.
/// </summary>
public static class MemoNumberGenerator
{
    /// <summary>
    /// Generates a memo number using the specified username, date, and sequence number.
    /// This method is deterministic: the same inputs always produce the same output.
    /// </summary>
    /// <param name="username">
    /// The username portion of the memo number (e.g., "jsmith").
    /// Typically derived from the user's login name or email prefix.
    /// </param>
    /// <param name="date">
    /// The date to embed in the memo number. Usually the creation date (UTC).
    /// Only the date portion is used; the time component is ignored.
    /// </param>
    /// <param name="sequenceNumber">
    /// The sequential counter for this user on this date. Starts at 1.
    /// Zero-padded to 3 digits (e.g., 1 becomes "001", 12 becomes "012").
    /// </param>
    /// <returns>
    /// A formatted memo number string like "jsmith-20260302-001".
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when username is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sequenceNumber is less than 1.</exception>
    public static string Generate(string username, DateTime date, int sequenceNumber)
    {
        // Validate inputs — never accept bad data and produce a confusing memo number
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username), "Username cannot be null or empty.");

        if (sequenceNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "Sequence number must be at least 1.");

        // Sanitise the username: convert to lowercase and strip any characters that are
        // not alphanumeric or a hyphen, to keep the memo number URL-safe and consistent.
        var sanitisedUsername = SanitiseUsername(username);

        // Format the date as YYYYMMDD (e.g., 2026-03-02 becomes "20260302")
        var datePart = date.ToString("yyyyMMdd");

        // Zero-pad the sequence to 3 digits (e.g., 1 → "001", 99 → "099", 100 → "100")
        var sequencePart = sequenceNumber.ToString("D3");

        // Combine all parts with hyphens: [username]-[YYYYMMDD]-[seq]
        return $"{sanitisedUsername}-{datePart}-{sequencePart}";
    }

    /// <summary>
    /// Sanitises a username for inclusion in a memo number.
    /// Converts to lowercase and removes characters that are not letters, digits, or hyphens.
    /// </summary>
    /// <param name="username">The raw username to sanitise.</param>
    /// <returns>A lowercase, URL-safe version of the username.</returns>
    public static string SanitiseUsername(string username)
    {
        // Convert to lowercase for consistency (memo numbers are always lowercase)
        var lower = username.ToLowerInvariant();

        // Keep only letters, digits, and hyphens — remove everything else
        // This prevents special characters from breaking the format or causing security issues
        var sanitised = new string(lower.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // If after sanitisation the username is empty, use "user" as a safe fallback
        return string.IsNullOrEmpty(sanitised) ? "user" : sanitised;
    }
}
