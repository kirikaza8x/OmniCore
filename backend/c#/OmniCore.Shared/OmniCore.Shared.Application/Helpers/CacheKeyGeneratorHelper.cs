namespace OmniCore.Shared.Application.Helpers;

public static class CacheKeyGeneratorHelper
{
    public static string For<T>(params object[] identifiers)
    {
        var typeName = typeof(T).Name;
        if (identifiers.Length == 0)
        {
            return typeName;
        }

        return $"{typeName}:{string.Join(":", identifiers)}";
    }
}