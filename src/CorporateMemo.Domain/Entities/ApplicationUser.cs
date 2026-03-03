using Microsoft.AspNetCore.Identity;

namespace CorporateMemo.Domain.Entities;

/// <summary>
/// Extends the default ASP.NET Core Identity user with additional profile information.
/// This is the user entity stored in the database and used for authentication.
/// IdentityUser provides: Id, Email, UserName, PasswordHash, and many other built-in fields.
/// We add: DisplayName and Department for the corporate context.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's full display name (e.g., "John Smith").
    /// This is shown in the UI wherever the user's identity is displayed.
    /// Auto-populated in memo Author fields when a memo is created.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's department within the organisation (e.g., "Finance", "HR").
    /// Optional. Can be used for filtering or routing in future versions.
    /// </summary>
    public string? Department { get; set; }
}
