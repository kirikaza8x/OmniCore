namespace OmniCore.Shared.Application.Behaviors;

using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Rejects a tenant-scoped request (<see cref="ITenantScopedRequest"/>) that has no active tenant (ADR 0001).
/// Handlers never take a tenant id from the client: they read <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantBehavior<TRequest, TResponse>(ITenantContext tenantContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ITenantScopedRequest
    where TResponse : Result
{
    public static readonly Error NoTenant = Error.Forbidden("Tenant.Required", "No active tenant for this request.");

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (tenantContext.IsSystem || tenantContext.TenantId is not null)
        {
            return next();
        }

        return Task.FromResult(FailureFactory.Create<TResponse>(NoTenant));
    }
}

internal static class FailureFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Cache = new();

    public static TResponse Create<TResponse>(Error error)
        where TResponse : Result
    {
        Func<Error, object> factory = Cache.GetOrAdd(typeof(TResponse), type =>
        {
            if (type == typeof(Result))
            {
                return e => Result.Failure(e);
            }

            MethodInfo generic = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == nameof(Result.Failure)
                             && m.IsGenericMethodDefinition
                             && m.GetParameters().Length == 1
                             && m.GetParameters()[0].ParameterType == typeof(Error));

            MethodInfo closed = generic.MakeGenericMethod(type.GenericTypeArguments[0]);
            return e => closed.Invoke(null, [e])!;
        });

        return (TResponse)factory(error);
    }
}
