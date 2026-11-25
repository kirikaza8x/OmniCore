namespace OmniCore.Services.Auth.Application.Features.Auth.Commands.Register;

/// <summary>
/// Usernames that must not be claimed through registration because they look like system or support identities.
/// The first-admin seeder builds accounts through the domain, so it can still use a reserved name (for example "admin").
/// </summary>
public static class ReservedUsernames
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "administrator",
        "root",
        "superadmin",
        "system",
        "support",
        "moderator",
        "owner",
        "null",
        "undefined",
        "api",
        "www",
        "me",
        "anonymous",
        "postmaster",
        "noreply",
        "no-reply"
    };

    public static bool IsReserved(string? username) =>
        !string.IsNullOrEmpty(username) && Names.Contains(username);
}
