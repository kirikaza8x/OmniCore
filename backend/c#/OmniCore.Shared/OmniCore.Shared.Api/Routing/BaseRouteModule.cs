namespace OmniCore.Shared.Api.Routing;

using System.Linq;

/// <summary>
/// Provides a base routing module with global conventions for API endpoints.
/// Dynamically filters and builds route paths based on configured segments.
/// </summary>
public abstract class BaseRouteModule
{
    /// <summary>
    /// Global API prefix applied to all routes.
    /// Default value: <c>"api"</c>.
    /// </summary>
    protected virtual string? ApiPrefix => "api";

    /// <summary>
    /// API version applied to all routes.
    /// Default value: <c>"v1"</c>.
    /// </summary>
    protected virtual string? Version => "v1";

    /// <summary>
    /// Optional service context name (e.g., "auth", "ordering").
    /// Default value: <c>null</c>.
    /// </summary>
    protected virtual string? ServiceName => null;

    /// <summary>
    /// Resource name for the module (e.g., "roles", "users").
    /// Must be defined by child classes, or returned as null/empty if the group is root-level.
    /// </summary>
    protected abstract string? ResourceName { get; }

    /// <summary>
    /// Dynamically constructs the base route prefix, omitting null or empty path segments.
    /// Examples:
    /// <list type="bullet">
    /// <item><c>"api/v1/auth/roles"</c> (when ServiceName is "auth" and ResourceName is "roles")</item>
    /// <item><c>"api/v1/roles"</c> (when ServiceName is null and ResourceName is "roles")</item>
    /// </list>
    /// </summary>
    public string GroupPrefix
    {
        get
        {
            var segments = new[] { ApiPrefix, Version, ServiceName, ResourceName }
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => segment!.Trim('/'));

            return string.Join('/', segments);
        }
    }

    /// <summary>
    /// Standard sub-path for retrieving a resource by its unique identifier.
    /// Example: <c>"{id:guid}"</c>.
    /// </summary>
    public virtual string GetById => "{id:guid}";

    /// <summary>
    /// Standard sub-path for retrieving all resources.
    /// Default value: empty string.
    /// </summary>
    public virtual string GetAll => "";

    /// <summary>
    /// Standard sub-path for creating a new resource.
    /// Default value: empty string.
    /// </summary>
    public virtual string Create => "";

    /// <summary>
    /// Standard sub-path for updating an existing resource by its unique identifier.
    /// Example: <c>"{id:guid}"</c>.
    /// </summary>
    public virtual string Update => "{id:guid}";

    /// <summary>
    /// Standard sub-path for deleting a resource by its unique identifier.
    /// Example: <c>"{id:guid}"</c>.
    /// </summary>
    public virtual string Delete => "{id:guid}";

    /// <summary>
    /// Automatically derives the Swagger tag name from ResourceName, falling back to ServiceName.
    /// Example: <c>"roles"</c> → <c>"Roles"</c>.
    /// </summary>
    public virtual string Tag
    {
        get
        {
            var target = !string.IsNullOrWhiteSpace(ResourceName) 
                ? ResourceName 
                : ServiceName;

            if (string.IsNullOrWhiteSpace(target))
            {
                return "Default";
            }

            var trimmed = target.Trim();
            return char.ToUpper(trimmed[0]) + trimmed[1..];
        }
    }
}