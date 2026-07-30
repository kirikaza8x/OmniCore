namespace OmniCore.Shared.Application.Behaviors;

using System.Collections.Concurrent;
using System.Linq.Expressions;
using FluentValidation;
using MediatR;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Pipeline behavior for automatically running FluentValidation rules before command execution.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();

        if (failures.Length == 0)
        {
            return await next();
        }

        var errors = failures
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .ToArray();

        return ValidationResultFactory.Create<TResponse>(errors);
    }
}

/// <summary>
/// High-performance cached factory for generating ValidationResult instances without repeated reflection.
/// </summary>
internal static class ValidationResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error[], object>> FactoryCache = new();

    public static TResponse Create<TResponse>(Error[] errors)
        where TResponse : Result
    {
        Type responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (ValidationResult.WithErrors(errors) as TResponse)!;
        }

        Func<Error[], object> factory = FactoryCache.GetOrAdd(responseType, type =>
        {
            Type valueType = type.GenericTypeArguments[0];
            Type validationResultType = typeof(ValidationResult<>).MakeGenericType(valueType);
            var method = validationResultType.GetMethod(nameof(ValidationResult.WithErrors), new[] { typeof(Error[]) });

            var parameter = Expression.Parameter(typeof(Error[]), "errors");
            var call = Expression.Call(method!, parameter);
            var lambda = Expression.Lambda<Func<Error[], object>>(call, parameter);

            return lambda.Compile();
        });

        return (TResponse)factory(errors);
    }
}