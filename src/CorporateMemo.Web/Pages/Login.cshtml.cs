using CorporateMemo.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CorporateMemo.Web.Pages;

/// <summary>
/// Razor Page model for the login form.
///
/// WHY A RAZOR PAGE NOT A BLAZOR COMPONENT?
/// ASP.NET Core Identity's SignInManager.PasswordSignInAsync() sets an
/// authentication cookie by writing to HTTP response headers.
/// In Blazor Server, the HTTP response is already committed before any
/// interactive event fires (Blazor uses SignalR after the first page load).
/// A Razor Page runs on a real HTTP POST request, so headers are still open
/// and the cookie can be written successfully.
/// </summary>
public class LoginPageModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginPageModel> _logger;

    /// <summary>
    /// Constructor — dependencies are injected by ASP.NET Core DI.
    /// </summary>
    public LoginPageModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginPageModel> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    /// <summary>
    /// The form fields bound from the HTTP POST body.
    /// [BindProperty] tells the framework to populate this from the form values on POST.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// The URL to redirect to after a successful login.
    /// Passed as a query-string parameter by the auth middleware when it
    /// redirects an unauthenticated user to this page.
    /// </summary>
    public string ReturnUrl { get; set; } = "/";

    /// <summary>
    /// Error message shown in the UI when login fails (wrong password, locked, etc.)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Form input model — contains only the fields needed for login.
    /// </summary>
    public class InputModel
    {
        /// <summary>User's email address (used as the username).</summary>
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>User's password.</summary>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handles GET /login — just renders the form.
    /// Stores the returnUrl so the form can POST it back.
    /// </summary>
    /// <param name="returnUrl">Where to send the user after login.</param>
    public void OnGet(string? returnUrl = null)
    {
        // Default to the dashboard root if no returnUrl is specified
        ReturnUrl = returnUrl ?? "/";
    }

    /// <summary>
    /// Handles POST /login — validates the form, attempts sign-in, sets the cookie.
    ///
    /// Flow:
    /// 1. Validate the form (ModelState)
    /// 2. Call SignInManager.PasswordSignInAsync — checks credentials and sets auth cookie
    /// 3. On success: redirect to ReturnUrl
    /// 4. On failure: show error message, re-render the form
    /// </summary>
    /// <param name="returnUrl">Where to redirect after successful login.</param>
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        // Ensure we only redirect to local URLs (prevent open redirect attacks)
        returnUrl = returnUrl != null && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        ReturnUrl = returnUrl;

        // If form validation failed (e.g. empty email), re-render the form with errors
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Attempt to sign in with the provided credentials.
        // PasswordSignInAsync will:
        //   - Look up the user by username (email in our case)
        //   - Verify the hashed password
        //   - Write the auth cookie to the HTTP response (this is why we need a Razor Page)
        //   - Apply lockout rules if configured
        var result = await _signInManager.PasswordSignInAsync(
            userName: Input.Email,
            password: Input.Password,
            isPersistent: false,        // Session cookie — expires when the browser closes
            lockoutOnFailure: true);    // Lock the account after 5 bad attempts (configured in Program.cs)

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in successfully.", Input.Email);
            // LocalRedirect prevents open redirect vulnerabilities
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked for user {Email}.", Input.Email);
            ErrorMessage = "Your account has been locked due to too many failed attempts. Try again in 15 minutes.";
        }
        else
        {
            // Deliberately vague — don't tell the attacker whether the email exists
            _logger.LogWarning("Failed login attempt for {Email}.", Input.Email);
            ErrorMessage = "Invalid email or password. Please check your credentials and try again.";
        }

        // Re-render the page with the error message
        return Page();
    }
}
