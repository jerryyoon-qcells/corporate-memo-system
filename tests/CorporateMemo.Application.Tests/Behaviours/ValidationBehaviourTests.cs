using CorporateMemo.Application.Behaviours;
using CorporateMemo.Application.Memos.Commands;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace CorporateMemo.Application.Tests.Behaviours;

/// <summary>
/// Unit tests for the <see cref="ValidationBehaviour{TRequest,TResponse}"/> MediatR pipeline behaviour.
///
/// Scenarios covered:
/// - Happy path: valid request with no validators → handler is called
/// - Happy path: valid request with validators → handler is called
/// - Validation failure: invalid request → ValidationException thrown, handler NOT called
/// - Multiple validators: failures from all validators are collected
/// </summary>
public class ValidationBehaviourTests
{
    // ============================================================
    // Test helpers — minimal MediatR request and next delegate
    // ============================================================

    /// <summary>
    /// A simple test request that carries a value we can validate.
    /// </summary>
    private record TestRequest(string Value) : IRequest<string>;

    /// <summary>
    /// Validator that requires Value to not be empty.
    /// </summary>
    private class NotEmptyValidator : AbstractValidator<TestRequest>
    {
        public NotEmptyValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty()
                .WithMessage("Value is required.");
        }
    }

    /// <summary>
    /// Validator that requires Value to be at least 3 characters.
    /// Used for multi-validator tests to ensure all errors are collected.
    /// </summary>
    private class MinLengthValidator : AbstractValidator<TestRequest>
    {
        public MinLengthValidator()
        {
            RuleFor(x => x.Value)
                .MinimumLength(3)
                .WithMessage("Value must be at least 3 characters.");
        }
    }

    // Tracks how many times the inner handler (next) was called
    private int _handlerCallCount;

    /// <summary>Creates a next-delegate that records being called and returns a fixed response.</summary>
    private RequestHandlerDelegate<string> MakeNext(string response = "ok")
    {
        return () =>
        {
            _handlerCallCount++;
            return Task.FromResult(response);
        };
    }

    // ============================================================
    // No validators registered — should pass through
    // ============================================================

    /// <summary>
    /// When no validators are registered for the request type, the behaviour should
    /// call the next delegate and return its result without throwing.
    /// </summary>
    [Fact]
    public async Task Handle_NoValidators_CallsNextAndReturnsResult()
    {
        // Arrange — empty validator collection means no validation rules
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            validators: Enumerable.Empty<IValidator<TestRequest>>(),
            logger: NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

        var request = new TestRequest("hello");

        // Act
        var result = await behaviour.Handle(request, MakeNext("result"), CancellationToken.None);

        // Assert
        result.Should().Be("result");
        _handlerCallCount.Should().Be(1);
    }

    // ============================================================
    // Valid request — should call next
    // ============================================================

    /// <summary>
    /// When validation passes, the next delegate should be called once and its result returned.
    /// </summary>
    [Fact]
    public async Task Handle_ValidRequest_CallsNextAndReturnsResult()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[] { new NotEmptyValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            validators, NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

        var request = new TestRequest("valid-value");

        // Act
        var result = await behaviour.Handle(request, MakeNext("ok"), CancellationToken.None);

        // Assert
        result.Should().Be("ok");
        _handlerCallCount.Should().Be(1);
    }

    // ============================================================
    // Invalid request — should throw ValidationException
    // ============================================================

    /// <summary>
    /// When validation fails, a FluentValidation.ValidationException should be thrown
    /// and the next delegate should NOT be called.
    /// </summary>
    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        // Arrange — empty string will fail the NotEmpty rule
        var validators = new IValidator<TestRequest>[] { new NotEmptyValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            validators, NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

        var request = new TestRequest(""); // empty value — fails validation

        // Act & Assert
        var act = () => behaviour.Handle(request, MakeNext(), CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// When validation fails, the inner handler (next) should NOT be called.
    /// </summary>
    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallNext()
    {
        // Arrange
        var validators = new IValidator<TestRequest>[] { new NotEmptyValidator() };
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            validators, NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

        var request = new TestRequest("");

        // Act — swallow the exception for this test
        try { await behaviour.Handle(request, MakeNext(), CancellationToken.None); }
        catch (ValidationException) { /* expected */ }

        // Assert
        _handlerCallCount.Should().Be(0);
    }

    /// <summary>
    /// When multiple validators each report failures, ALL errors should be collected
    /// into the single thrown ValidationException.
    /// </summary>
    [Fact]
    public async Task Handle_MultipleValidatorsWithFailures_CollectsAllErrors()
    {
        // Arrange — "x" is non-empty but is only 1 character (< 3) → fails MinLengthValidator only
        //            "" fails both NotEmpty AND MinLength
        var validators = new IValidator<TestRequest>[]
        {
            new NotEmptyValidator(),
            new MinLengthValidator()
        };
        var behaviour = new ValidationBehaviour<TestRequest, string>(
            validators, NullLogger<ValidationBehaviour<TestRequest, string>>.Instance);

        var request = new TestRequest(""); // fails both validators

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(request, MakeNext(), CancellationToken.None));

        // Both error messages should be present in the collected failures
        var errorMessages = ex.Errors.Select(e => e.ErrorMessage).ToList();
        errorMessages.Should().Contain("Value is required.");
        errorMessages.Should().Contain("Value must be at least 3 characters.");
    }

    // ============================================================
    // Integration: CreateMemoCommandValidator via ValidationBehaviour
    // ============================================================

    /// <summary>
    /// A CreateMemoCommand with an empty Title should fail the real validator.
    /// This integration-style test verifies the actual production validator works
    /// when wired through the pipeline behaviour.
    /// </summary>
    [Fact]
    public async Task Handle_CreateMemoCommandWithEmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var validators = new IValidator<CreateMemoCommand>[] { new CreateMemoCommandValidator() };
        var behaviour = new ValidationBehaviour<CreateMemoCommand, DTOs.MemoDto>(
            validators,
            NullLogger<ValidationBehaviour<CreateMemoCommand, DTOs.MemoDto>>.Instance);

        // Title is empty — should fail the "Title is required" rule
        var command = new CreateMemoCommand { Title = "", Content = "Some content" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(command, () => Task.FromResult(new DTOs.MemoDto()), CancellationToken.None));

        ex.Errors.Should().Contain(e => e.ErrorMessage.Contains("title"));
    }

    /// <summary>
    /// A CreateMemoCommand with a title exceeding 100 characters should fail validation.
    /// </summary>
    [Fact]
    public async Task Handle_CreateMemoCommandWithTitleTooLong_ThrowsValidationException()
    {
        // Arrange
        var validators = new IValidator<CreateMemoCommand>[] { new CreateMemoCommandValidator() };
        var behaviour = new ValidationBehaviour<CreateMemoCommand, DTOs.MemoDto>(
            validators,
            NullLogger<ValidationBehaviour<CreateMemoCommand, DTOs.MemoDto>>.Instance);

        var command = new CreateMemoCommand
        {
            Title = new string('A', 101), // 101 characters — exceeds maximum
            Content = "Some content"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => behaviour.Handle(command, () => Task.FromResult(new DTOs.MemoDto()), CancellationToken.None));
    }
}
