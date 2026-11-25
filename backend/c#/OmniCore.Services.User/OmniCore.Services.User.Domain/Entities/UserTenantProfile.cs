namespace OmniCore.Services.User.Domain.Entities;

using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

/// <summary>
/// What a person is called inside one tenant (ADR 0001). One row per (user, tenant); the tenant comes from the token and
/// the EF filter keeps other tenants' rows out of every query. Created on the first save, not at registration.
/// </summary>
public class UserTenantProfile : TenantAggregateRoot<UserTenantProfileId>
{
    public const int MaxGamerTagLength = 50;
    public const int MaxAvatarUrlLength = 2048;

    public UserId UserId { get; private set; } = null!;
    public string GamerTag { get; private set; } = string.Empty;
    public string? CustomAvatarUrl { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    private UserTenantProfile() { }

    private UserTenantProfile(
        UserTenantProfileId id,
        TenantId tenantId,
        UserId userId,
        string gamerTag,
        string? customAvatarUrl,
        DateTime joinedAtUtc) : base(id, tenantId)
    {
        UserId = userId;
        GamerTag = gamerTag;
        CustomAvatarUrl = customAvatarUrl;
        JoinedAtUtc = joinedAtUtc;
    }

    public static Result<UserTenantProfile> Create(
        TenantId tenantId,
        UserId userId,
        string gamerTag,
        string? customAvatarUrl,
        DateTime joinedAtUtc)
    {
        if (userId is null)
        {
            return Result.Failure<UserTenantProfile>(UserProfileErrors.Unauthenticated);
        }

        Error? invalid = Validate(gamerTag, customAvatarUrl);
        if (invalid is not null) return Result.Failure<UserTenantProfile>(invalid);

        return new UserTenantProfile(
            UserTenantProfileId.New(), tenantId, userId, gamerTag.Trim(), NullIfBlank(customAvatarUrl), joinedAtUtc);
    }

    public Result Update(string gamerTag, string? customAvatarUrl)
    {
        Error? invalid = Validate(gamerTag, customAvatarUrl);
        if (invalid is not null) return Result.Failure(invalid);

        GamerTag = gamerTag.Trim();
        CustomAvatarUrl = NullIfBlank(customAvatarUrl);
        return Result.Success();
    }

    private static Error? Validate(string? gamerTag, string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(gamerTag)) return UserProfileErrors.GamerTagEmpty;
        if (gamerTag.Trim().Length > MaxGamerTagLength) return UserProfileErrors.TooLong("GamerTag", MaxGamerTagLength);
        if (NullIfBlank(avatarUrl)?.Length > MaxAvatarUrlLength) return UserProfileErrors.TooLong("CustomAvatarUrl", MaxAvatarUrlLength);
        return null;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
