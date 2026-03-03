using CorporateMemo.Application.Interfaces;
using CorporateMemo.Infrastructure.Configuration;
using CorporateMemo.Infrastructure.Data;
using CorporateMemo.Infrastructure.Repositories;
using CorporateMemo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Infrastructure;

/// <summary>
/// Extension method class for registering Infrastructure layer services into the DI container.
/// This follows the "service registration extension" pattern where each layer registers its own services.
/// The Web layer (Program.cs) calls this method: <c>builder.Services.AddInfrastructureServices(config)</c>
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure layer services into the dependency injection container.
    ///
    /// Registers:
    /// - ApplicationDbContext (EF Core + SQL Server)
    /// - MemoRepository
    /// - EmailService (MailKit)
    /// - NotificationService
    /// - LocalAttachmentStorage
    /// - Options bindings for SmtpSettings and AttachmentSettings
    /// </summary>
    /// <param name="services">The IServiceCollection to register services into.</param>
    /// <param name="configuration">
    /// The application configuration (appsettings.json).
    /// Used to read connection strings and settings sections.
    /// </param>
    /// <returns>The same IServiceCollection for method chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register EF Core with SQL Server provider
        // The connection string is named "DefaultConnection" in appsettings.json
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                // Configure SQL Server options — retry on transient failures (network glitches, etc.)
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null));
        });

        // Register repositories — Scoped means one instance per HTTP request
        // This is important because the DbContext is also scoped
        services.AddScoped<IMemoRepository, MemoRepository>();

        // Register services — Scoped for consistency with the DbContext lifetime
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAttachmentStorage, LocalAttachmentStorage>();

        // Bind the SmtpSettings configuration section to the strongly-typed class
        // Inject as IOptions<SmtpSettings> in EmailService constructor
        // Using the standard IServiceCollection.Configure<T> with GetSection
        services.Configure<SmtpSettings>(opts =>
            configuration.GetSection("SmtpSettings").Bind(opts));

        // Bind the AttachmentSettings configuration section to the strongly-typed class
        services.Configure<AttachmentSettings>(opts =>
            configuration.GetSection("AttachmentSettings").Bind(opts));

        // Expose IAttachmentSettings to the Application layer so validators can inject it
        // without taking a direct dependency on the Infrastructure AttachmentSettings class.
        // We resolve it from IOptions<AttachmentSettings> so the same configured instance is reused.
        services.AddScoped<IAttachmentSettings>(sp =>
            sp.GetRequiredService<IOptions<AttachmentSettings>>().Value);

        return services;
    }
}
