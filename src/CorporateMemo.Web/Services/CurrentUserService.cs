using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CorporateMemo.Web.Services;

/// <summary>
/// ASP.NET Core implementation of <see cref="ICurrentUserService"/>.
/// Uses IHttpContextAccessor to access the current HTTP context and retrieve
/// the authenticated user's claims.
///
/// In Blazor Server, HTTP context is available during the initial connection
/// but not during subsequent SignalR interactions. For Blazor Server,
/// we use AuthenticationStateProvider in the Blazor components directly.
/// This service is used in command/query handlers where HTTP context is available.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Initializes the service with the HTTP context accessor and user manager.
    /// </summary>
    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets the user's unique ID from the NameIdentifier claim.
    /// Returns null if the user is not authenticated.
    /// </summary>
    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Gets the user's username (login name) from the Name claim.
    /// </summary>
    public string? UserName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Name);

    /// <summary>
    /// Gets the user's email address from the Email claim.
    /// </summary>
    public string? UserEmail =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);

    /// <summary>
    /// Gets the user's display name from the GivenName claim, or falls back to the Name claim.
    /// The display name is stored when the user registers/logs in.
    /// </summary>
    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("DisplayName")
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.GivenName)
        ?? UserName;

    /// <summary>
    /// Gets whether the current user is in the "Admin" role.
    /// </summary>
    public bool IsAdmin =>
        _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
}
