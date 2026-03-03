using CorporateMemo.Application.Behaviours;
using CorporateMemo.Application.Mappings;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CorporateMemo.Application;

/// <summary>
/// Extension method class for registering Application layer services into the DI container.
/// This follows the "service registration extension" pattern where each layer registers its own services.
/// The Web layer (Program.cs) calls this method, keeping registration logic close to the code it configures.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all Application layer services into the dependency injection container.
    /// Call this from Program.cs: <c>builder.Services.AddApplicationServices()</c>
    ///
    /// Registers:
    /// - MediatR (for CQRS command/query dispatching)
    /// - AutoMapper (for entity-to-DTO mapping)
    /// - FluentValidation (for all validators in this assembly)
    /// - ValidationBehaviour (the MediatR pipeline behaviour for validation)
    /// </summary>
    /// <param name="services">The IServiceCollection to register services into.</param>
    /// <returns>The same IServiceCollection for method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR — this scans the Application assembly for all IRequestHandler implementations
        // and registers them automatically. When mediator.Send(command) is called, MediatR finds
        // the correct handler and invokes it.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

            // Register our validation pipeline behaviour
            // This runs FluentValidation BEFORE every handler
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        // Register AutoMapper — this scans the Application assembly for all Profile classes
        // (like MemoMappingProfile) and registers their mappings automatically
        services.AddAutoMapper(typeof(MemoMappingProfile).Assembly);

        // Register FluentValidation — this scans the Application assembly for all AbstractValidator<T>
        // implementations and registers them. The ValidationBehaviour will then find them via DI.
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        return services;
    }
}
