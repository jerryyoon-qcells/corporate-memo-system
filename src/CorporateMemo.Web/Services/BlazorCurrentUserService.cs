using CorporateMemo.Application.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace CorporateMemo.Web.Services;

/// <summary>
/// Blazor Server-safe implementation of <see cref="ICurrentUserService"/>.
///
/// WHY THIS EXISTS:
/// In Blazor Server, the HTTP context is only available during the initial page-load
/// HTTP request. Once the SignalR circuit is established, all subsequent interactions
/// (button clicks, form submits, etc.) run over SignalR — not HTTP — so
/// <c>IHttpContextAccessor.HttpContext</c> returns null for every command dispatched
/// from a Blazor component interaction. This causes every authorization guard in the
/// command handlers to throw or return null.
///
/// FIX:
/// This service is Scoped (one instance per Blazor circuit). <c>MainLayout.razor</c>
/// calls <see cref="Initialize"/> once in <c>OnInitializedAsync</c>, passing the
/// <see cref="AuthenticationState"/> that is correctly available throughout the
/// SignalR circuit lifetime. After initialization, the stored <see cref="ClaimsPrincipal"/>
/// is used to answer all identity queries, regardless of whether an HTTP context exists.
/// </summary>
public class BlazorCurrentUserService : ICurrentUserService
{
    // Stored once per circuit during Initialize()
    private ClaimsPrincipal? _user;

    /// <summary>
    /// Called by MainLayout.razor in OnInitializedAsync to capture the current
    /// authentication state for the duration of this Blazor circuit.
    /// </summary>
    /// <param name="authState">The authentication state provided by the Blazor auth system.</param>
    public void Initialize(AuthenticationState authState)
    {
        _user = authState.User;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Reads from the stored ClaimsPrincipal, which remains valid throughout the
    /// SignalR circuit lifetime even after the initial HTTP context has been disposed.
    /// </remarks>
    public string? UserId =>
        _user?.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc/>
    public string? UserName =>
        _user?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc/>
    public string? UserEmail =>
        _user?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    /// <remarks>
    /// Falls back to the GivenName claim and then to the Name claim if no DisplayName
    /// custom claim is present, matching the original service's fallback behaviour.
    /// </remarks>
    public string? DisplayName =>
        _user?.FindFirstValue("DisplayName")
        ?? _user?.FindFirstValue(ClaimTypes.GivenName)
        ?? UserName;

    /// <inheritdoc/>
    public bool IsAdmin =>
        _user?.IsInRole("Admin") ?? false;
}
