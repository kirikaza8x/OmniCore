namespace OmniCore.Shared.Application.Helpers;

using System;
using System.Linq;

public static class CacheKeyGeneratorHelper
{
    /// <summary>
    /// Generates a cache key using a generic target type and identifiers.
    /// Example: CacheKeyGeneratorHelper.For<GetUserProfileByIdQuery>(userId) -> "GetUserProfileByIdQuery:00000000-0000-0000-0000-000000000000"
    /// </summary>
    public static string For<T>(params object[] identifiers)
    {
        return For(typeof(T), identifiers);
    }

    /// <summary>
    /// Generates a cache key dynamically using a System.Type instance.
    /// Useful for runtime type resolution or generic reflection handlers.
    /// </summary>
    public static string For(Type type, params object[] identifiers)
    {
        string typeName = type.Name;

        if (identifiers.Length == 0)
        {
            return typeName;
        }

        var filterNulls = identifiers.Where(id => id is not null);
        return $"{typeName}:{string.Join(":", filterNulls)}";
    }

    /// <summary>
    /// Generates a cache prefix suitable for wildcard/prefix invalidation using ICacheService.RemoveByPrefixAsync.
    /// Example: CacheKeyGeneratorHelper.PrefixFor<GetUserProfileByIdQuery>() -> "GetUserProfileByIdQuery:"
    /// </summary>
    public static string PrefixFor<T>()
    {
        return PrefixFor(typeof(T));
    }

    /// <summary>
    /// Generates a cache prefix dynamically using a System.Type instance.
    /// Example: CacheKeyGeneratorHelper.PrefixFor(typeof(GetUserProfileByIdQuery)) -> "GetUserProfileByIdQuery:"
    /// </summary>
    public static string PrefixFor(Type type)
    {
        return $"{type.Name}:";
    }
}