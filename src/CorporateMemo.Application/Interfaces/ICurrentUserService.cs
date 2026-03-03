namespace CorporateMemo.Application.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user.
/// This interface allows the Application layer to access user identity information
/// without directly depending on ASP.NET Core's HTTP context (which is an Infrastructure concern).
/// The Web layer implements this using IHttpContextAccessor.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// This is the same ID as the ApplicationUser.Id (a Guid stored as a string by Identity).
    /// Returns null if the user is not authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the username (login name) of the currently authenticated user.
    /// This is typically the email address prefix used as the username.
    /// Returns null if the user is not authenticated.
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Gets the email address of the currently authenticated user.
    /// Returns null if the user is not authenticated.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Gets the display name of the currently authenticated user (e.g., "John Smith").
    /// Returns null if the user is not authenticated or does not have a display name set.
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets a value indicating whether the current user has administrator privileges.
    /// Admins can view and manage all memos regardless of ownership.
    /// </summary>
    bool IsAdmin { get; }
}
