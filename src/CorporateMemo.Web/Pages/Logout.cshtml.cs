using CorporateMemo.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CorporateMemo.Web.Pages;

/// <summary>
/// Razor Page that handles POST /logout.
///
/// WHY A RAZOR PAGE INSTEAD OF A BLAZOR COMPONENT?
/// Blazor Server runs over a persistent SignalR WebSocket connection.
/// By the time a user clicks a button in a Blazor component, the original
/// HTTP response has already been sent and its headers are sealed.
/// Clearing an authentication cookie requires writing to HTTP response headers,
/// so it MUST happen during a real HTTP request — not over SignalR.
///
/// WHY [IgnoreAntiforgeryToken]?
/// MainLayout.razor is a Blazor component and cannot use @Html.AntiForgeryToken()
/// (that helper is only available in Razor Views/Pages, not Blazor components).
/// Logout CSRF risk is minimal — the worst an attacker can do is force the
/// victim to sign out, which is not a privilege escalation.
/// </summary>
[IgnoreAntiforgeryToken]
public class LogoutPageModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LogoutPageModel> _logger;

    /// <summary>Constructor — dependencies are injected by ASP.NET Core DI.</summary>
    public LogoutPageModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LogoutPageModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// Handles POST /logout — signs the user out and redirects to the login page.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out.");
        return LocalRedirect("/login");
    }
}
