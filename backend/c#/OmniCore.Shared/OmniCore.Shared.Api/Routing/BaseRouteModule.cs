namespace OmniCore.Shared.Api.Routing
{
    /// <summary>
    /// Provides a base routing module with global conventions for API endpoints.
    /// Child classes must define their resource name and can override default paths.
    /// </summary>
    public abstract class BaseRouteModule
    {
        /// <summary>
        /// Global API prefix applied to all routes.
        /// Default value: <c>"api"</c>.
        /// </summary>
        protected virtual string ApiPrefix => "api";

        /// <summary>
        /// API version applied to all routes.
        /// Default value: <c>"v1"</c>.
        /// </summary>
        protected virtual string Version => "v1";

        /// <summary>
        /// Resource name for the module (e.g., "auth", "orders").
        /// Must be defined by child classes.
        /// </summary>
        protected abstract string ResourceName { get; }

        /// <summary>
        /// Generates the base group path for the resource.
        /// Example: <c>"api/v1/auth"</c>.
        /// </summary>
        public string GroupPrefix => $"{ApiPrefix}/{Version}/{ResourceName.ToLowerInvariant()}";

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
        /// Automatically derives the Swagger tag name from the resource.
        /// Example: <c>"auth"</c> → <c>"Auth"</c>.
        /// </summary>
        public virtual string Tag => char.ToUpper(ResourceName[0]) + ResourceName[1..];
    }
}
