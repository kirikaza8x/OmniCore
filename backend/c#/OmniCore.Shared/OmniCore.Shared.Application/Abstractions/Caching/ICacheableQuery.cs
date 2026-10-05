using OmniCore.Shared.Application.Abstractions.Messaging;

namespace OmniCore.Shared.Application.Abstractions.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}
public interface ICacheableQuery<TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Expiration => null;
}