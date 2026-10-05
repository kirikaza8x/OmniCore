namespace OmniCore.Shared.Infrastructure.Extensions;

using System.Reflection;
using OmniCore.Shared.Contracts.Events;

internal static class EventBusReflectionExtensions
{
    public static IEnumerable<Type> GetIntegrationEventTypes(this IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IIntegrationEvent).IsAssignableFrom(t));

    public static IEnumerable<Type> GetHandledEventTypes(this IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
            .Select(i => i.GetGenericArguments()[0])
            .Distinct();

    public static string ToKebabCase(this string str) =>
        System.Text.RegularExpressions.Regex
            .Replace(str, "(?<!^)([A-Z])", "-$1")
            .ToLowerInvariant();
}