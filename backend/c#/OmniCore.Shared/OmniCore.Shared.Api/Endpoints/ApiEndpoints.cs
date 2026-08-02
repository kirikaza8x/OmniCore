namespace OmniCore.Shared.Api.Endpoints;

/// <summary>
/// Centralized endpoint registry for standardized API routing, tags, and parameters.
/// </summary>
public static class ApiEndpoints
{
    private const string ApiBase = "api/v{version:apiVersion}";

    public static class Auth
    {
        public const string Tag = "Authentication";
        private const string Base = $"{ApiBase}/auth";

        public const string Login = $"{Base}/login";
        public const string Register = $"{Base}/register";
        public const string RefreshToken = $"{Base}/refresh-token";
        public const string RevokeToken = $"{Base}/revoke-token";
    }

    public static class Orders
    {
        public const string Tag = "Orders";
        private const string Base = $"{ApiBase}/orders";

        public const string Create = Base;
        public const string GetAll = Base;
        public const string GetById = $"{Base}/{{id:guid}}";
        public const string Cancel = $"{Base}/{{id:guid}}/cancel";
    }

    public static class Products
    {
        public const string Tag = "Products";
        private const string Base = $"{ApiBase}/products";

        public const string Create = Base;
        public const string GetAll = Base;
        public const string GetById = $"{Base}/{{id:guid}}";
        public const string Update = $"{Base}/{{id:guid}}";
        public const string Delete = $"{Base}/{{id:guid}}";
    }

    public static class Webhooks
    {
        public const string Tag = "Webhooks";
        private const string Base = $"{ApiBase}/webhooks";

        public const string Stripe = $"{Base}/stripe";
    }
}