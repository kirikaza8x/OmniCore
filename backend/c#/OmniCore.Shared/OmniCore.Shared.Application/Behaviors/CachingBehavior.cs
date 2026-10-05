// Shared/Application/Behaviors/CachingBehavior.cs
namespace OmniCore.Shared.Application.Behaviors;

using MediatR;
using Microsoft.Extensions.Logging;
using OmniCore.Shared.Application.Abstractions.Caching;
using OmniCore.Shared.Domain.Abstractions;

/// <summary>
/// Pipeline behavior that automatically handles cache retrieval and population for queries implementing ICacheableQuery.
/// </summary>
internal sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string queryName = typeof(TRequest).Name;

        logger.LogDebug("Checking cache for query {QueryName} with key {CacheKey}", queryName, request.CacheKey);

        TResponse? response = await cacheService.GetOrCreateAsync(
            request.CacheKey,
            async ct => await next(),
            absoluteExpiration: request.Expiration,
            cancellationToken: cancellationToken);

        if (response is null || response.IsFailure)
        {
            logger.LogDebug("Query {QueryName} returned failure result; evicting from cache", queryName);
            await cacheService.RemoveAsync(request.CacheKey, cancellationToken);
        }
        else
        {
            logger.LogDebug("Cache hit or successfully cached result for query {QueryName}", queryName);
        }

        return response!;
    }
}