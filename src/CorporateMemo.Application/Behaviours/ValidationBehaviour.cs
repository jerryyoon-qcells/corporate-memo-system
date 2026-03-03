using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CorporateMemo.Application.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs FluentValidation validators before any command or query handler.
/// This is the "middleware" layer of the CQRS pipeline — it intercepts every request and validates it.
///
/// How MediatR pipelines work:
/// - When you call mediator.Send(command), MediatR builds a pipeline of behaviours
/// - Each behaviour can run code before and after the next step in the pipeline
/// - This ValidationBehaviour runs BEFORE the actual handler
/// - If validation fails, it throws an exception and the handler never runs
/// </summary>
/// <typeparam name="TRequest">The type of command or query being processed.</typeparam>
/// <typeparam name="TResponse">The return type of the command or query.</typeparam>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // Validators are injected via dependency injection
    // All IValidator<TRequest> implementations registered in DI will be here
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes the ValidationBehaviour with all validators registered for this request type.
    /// </summary>
    /// <param name="validators">
    /// All FluentValidation validators registered for the TRequest type.
    /// If no validators are registered for a particular command/query, this list will be empty
    /// and validation will be skipped (no error).
    /// </param>
    /// <param name="logger">Logger for recording validation failures.</param>
    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <summary>
    /// Handles the request by running all validators and then delegating to the next step in the pipeline.
    /// </summary>
    /// <param name="request">The command or query being processed.</param>
    /// <param name="next">A delegate representing the next step in the pipeline (the actual handler).</param>
    /// <param name="cancellationToken">A cancellation token to cancel the async operation if needed.</param>
    /// <returns>The response from the handler if validation passes.</returns>
    /// <exception cref="ValidationException">Thrown if any validator finds validation errors.</exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered for this request type, skip validation and proceed
        if (!_validators.Any())
            return await next();

        // Run all validators against the request object simultaneously
        // ValidationContext wraps the request object for FluentValidation
        var context = new ValidationContext<TRequest>(request);

        // Execute all validators asynchronously and collect the results
        // Task.WhenAll runs all validators in parallel for better performance
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all validation failures from all validators into one flat list
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(error => error != null) // Filter out null entries (just in case)
            .ToList();

        // If there are any validation failures, log them and throw an exception
        // The exception propagates up and prevents the handler from running
        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for {RequestType} with {FailureCount} error(s): {Errors}",
                typeof(TRequest).Name,
                failures.Count,
                string.Join(", ", failures.Select(f => f.ErrorMessage)));

            // FluentValidation's ValidationException automatically formats all errors
            throw new ValidationException(failures);
        }

        // All validation passed — proceed to the actual command/query handler
        return await next();
    }
}
