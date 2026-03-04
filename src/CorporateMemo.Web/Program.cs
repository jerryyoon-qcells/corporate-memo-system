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
// MaximumReceiveMessageSize is raised to 128 MB to support 100 MB file uploads.
builder.Services.AddServerSideBlazor(options =>
{
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
}).AddHubOptions(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 128 * 1024 * 1024; // 128 MB
});

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
// 5. APPLY DATABASE SCHEMA ON STARTUP
// ============================================================================
// For SQLite (development): EnsureCreated() creates the schema from the EF model
//   directly — no migration files needed, fast and zero-friction for dev.
// For SQL Server (production): MigrateAsync() applies pending migrations from
//   the Migrations folder in a controlled, versioned way.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CorporateMemo.Infrastructure.Data.ApplicationDbContext>();
    try
    {
        // EnsureCreated: creates all tables if the database is empty.
        // Idempotent — safe to run on every startup for both SQLite (dev) and
        // Azure SQL (production). For schema migrations in future, replace with
        // MigrateAsync() and add EF Core migration files.
        await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Database schema ensured.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while initialising the database.");
        throw; // Surface DB errors on startup so Azure health checks fail fast
    }
}

// ============================================================================
// 5b. SEED ROLES AND DEFAULT ADMIN USER
// ============================================================================
// On first run the database is empty. We create the standard roles and a default
// admin account so that someone can log in and set up the system.
// The seed is idempotent — it checks before inserting, so it is safe to run
// every time the application starts.
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // --- Create roles -------------------------------------------------------
    // Three roles: Admin, Collaborator, Viewer.
    string[] roles = ["Admin", "Collaborator", "Viewer"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            app.Logger.LogInformation("Created role: {Role}", role);
        }
    }

    // --- Rename legacy "User" role to "Collaborator" (idempotent) ----------
    // AspNetUserRoles links by RoleId, so renaming the row in AspNetRoles
    // automatically covers all existing user-role assignments.
    var legacyRole = await roleManager.FindByNameAsync("User");
    if (legacyRole is not null)
    {
        legacyRole.Name = "Collaborator";
        legacyRole.NormalizedName = "COLLABORATOR";
        var renameResult = await roleManager.UpdateAsync(legacyRole);
        if (renameResult.Succeeded)
            app.Logger.LogInformation("Renamed legacy role 'User' to 'Collaborator'.");
        else
            app.Logger.LogError("Failed to rename 'User' role: {Errors}",
                string.Join(", ", renameResult.Errors.Select(e => e.Description)));
    }

    // --- Create default admin account ---------------------------------------
    // This account is for first-time login / demo purposes.
    // Change the password immediately after first login in production.
    const string adminEmail    = "admin@corporatememo.local";
    const string adminPassword = "Admin1234";     // meets: 8+ chars, 1 digit

    if (await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var adminUser = new ApplicationUser
        {
            UserName    = adminEmail,
            Email       = adminEmail,
            DisplayName = "System Administrator",
            EmailConfirmed = true,              // skip email-confirmation step
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            app.Logger.LogInformation("Seed admin account created: {Email}", adminEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            app.Logger.LogError("Failed to create seed admin account: {Errors}", errors);
        }
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
