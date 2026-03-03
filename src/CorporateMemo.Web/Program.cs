using CorporateMemo.Application;
using CorporateMemo.Application.Interfaces;
using CorporateMemo.Domain.Entities;
using CorporateMemo.Infrastructure;
using CorporateMemo.Infrastructure.Data;
using CorporateMemo.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// =============================================================================
// Program.cs — Application Entry Point and DI Container Configuration
// =============================================================================
// This file configures all application services and middleware.
// It follows the .NET 8 minimal hosting model (no Startup.cs class needed).
//
// The configuration order matters:
// 1. Create the WebApplicationBuilder
// 2. Register all services (DI container setup)
// 3. Build the WebApplication
// 4. Configure middleware pipeline (in order: routing, auth, endpoints)
// 5. Run the application
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. DATABASE AND IDENTITY CONFIGURATION
// ============================================================================

// NOTE (M3 fix): The DbContext is registered ONLY inside AddInfrastructureServices()
// (called below in section 3), which includes the SQL Server retry-on-failure policy.
// A duplicate AddDbContext call was previously present here without retry configuration;
// that first registration would have silently won in the DI container and caused the
// retry policy in InfrastructureServiceExtensions to be ignored. It has been removed.

// Configure ASP.NET Core Identity for user authentication
// Identity provides: login, logout, password hashing, role management
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password policy — relaxed for MVP (tighten for production)
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    // Account lockout settings — lock account after 5 failed attempts
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Email settings — not required for MVP (no email confirmation)
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<CorporateMemo.Infrastructure.Data.ApplicationDbContext>()
// Add default token providers (needed for password reset, etc.)
.AddDefaultTokenProviders();

// ============================================================================
// 2. BLAZOR SERVER AND AUTHENTICATION CONFIGURATION
// ============================================================================

// Add Razor Pages — required by ASP.NET Core Identity for login/logout pages
builder.Services.AddRazorPages();

// Add MVC controllers — required for the AttachmentsController (C2 fix)
builder.Services.AddControllers();

// Add Blazor Server — this enables the interactive Blazor Server model
// Blazor Server renders components on the server and communicates via SignalR
builder.Services.AddServerSideBlazor();

// Enable authentication state in Blazor components
// This allows @attribute [Authorize] to work in .razor files
builder.Services.AddScoped<AuthenticationStateProvider,
    Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

// Enable HTTP context access (needed for CurrentUserService in Blazor Server)
builder.Services.AddHttpContextAccessor();

// Configure cookie-based authentication (used by Identity)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";        // Redirect unauthenticated users here
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.Cookie.Name = "CorporateMemo.Auth";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Session expires after 8 hours
    options.SlidingExpiration = true;   // Extends the session on each request
});

// ============================================================================
// 3. APPLICATION AND INFRASTRUCTURE SERVICES
// ============================================================================

// Register Application layer services (MediatR, AutoMapper, FluentValidation)
builder.Services.AddApplicationServices();

// Register Infrastructure layer services (EF Core repositories, Email, Storage)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register BlazorCurrentUserService as the ICurrentUserService implementation.
// Scoped: one instance per Blazor circuit (connection), which is correct because
// Initialize() is called once per circuit in MainLayout.razor.
//
// C3 fix: BlazorCurrentUserService uses AuthenticationState (captured via
// AuthenticationStateProvider in MainLayout.OnInitializedAsync) instead of
// IHttpContextAccessor, which is null after the initial HTTP request in Blazor Server.
builder.Services.AddScoped<ICurrentUserService, BlazorCurrentUserService>();

// ============================================================================
// 4. BUILD THE APPLICATION
// ============================================================================

var app = builder.Build();

// ============================================================================
// 5. APPLY DATABASE MIGRATIONS ON STARTUP
// ============================================================================
// This automatically applies any pending EF Core migrations when the app starts.
// In production, you may prefer to run migrations separately in CI/CD.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CorporateMemo.Infrastructure.Data.ApplicationDbContext>();
    try
    {
        // Apply all pending migrations automatically
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations.");
        // Don't throw — let the app continue; it may work with an existing schema
    }
}

// ============================================================================
// 6. MIDDLEWARE PIPELINE CONFIGURATION
// ============================================================================
// Order matters! Middleware is executed in the order it is added here.

if (!app.Environment.IsDevelopment())
{
    // In production: show a user-friendly error page instead of stack traces
    app.UseExceptionHandler("/Error");
    // Enforce HTTPS Strict Transport Security (tell browsers to always use HTTPS)
    app.UseHsts();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Serve static files from wwwroot (CSS, images, uploaded attachments)
app.UseStaticFiles();

// Enable routing so URLs are matched to endpoints
app.UseRouting();

// Authentication middleware — identifies who the user is (reads the auth cookie)
app.UseAuthentication();

// Authorization middleware — checks if the identified user is allowed to access the resource
app.UseAuthorization();

// Map Razor Pages (includes Identity UI pages like /Identity/Account/Login)
app.MapRazorPages();

// Map MVC controllers — exposes the AttachmentsController at /api/attachments/{id} (C2 fix)
app.MapControllers();

// Map the Blazor hub (the SignalR endpoint that Blazor uses to communicate with the browser)
app.MapBlazorHub();

// Map the fallback page — all non-matched routes are handled by the Blazor app
app.MapFallbackToPage("/_Host");

// ============================================================================
// 7. START THE APPLICATION
// ============================================================================
app.Run();
