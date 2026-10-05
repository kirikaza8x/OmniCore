// Entities/UserTenantProfile.cs
namespace OmniCore.Services.User.Domain.Entities;

using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class UserTenantProfile : TenantAggregateRoot<UserTenantProfileId>
{
    public UserId UserId { get; private set; } = null!;
    public string GamerTag { get; private set; } = string.Empty;
    public string? CustomAvatarUrl { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    public UserProfile UserProfile { get; private set; } = null!;

    private UserTenantProfile() { }

    private UserTenantProfile(
        UserTenantProfileId id,
        TenantId tenantId,
        UserId userId,
        string gamerTag,
        string? customAvatarUrl) : base(id, tenantId)
    {
        UserId = userId;
        GamerTag = gamerTag;
        CustomAvatarUrl = customAvatarUrl;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public static Result<UserTenantProfile> Create(
        TenantId tenantId,
        UserId userId,
        string gamerTag,
        string? customAvatarUrl = null)
    {
        if (userId is null)
        {
            return Result.Failure<UserTenantProfile>(Error.Validation("UserTenantProfile.UserIdRequired", "User ID is required."));
        }

        if (string.IsNullOrWhiteSpace(gamerTag))
        {
            return Result.Failure<UserTenantProfile>(Error.Validation("UserTenantProfile.GamerTagEmpty", "Gamer tag cannot be empty."));
        }

        return new UserTenantProfile(
            UserTenantProfileId.New(),
            tenantId,
            userId,
            gamerTag.Trim(),
            customAvatarUrl?.Trim());
    }

    public Result UpdateGamerTag(string newGamerTag)
    {
        if (string.IsNullOrWhiteSpace(newGamerTag))
        {
            return Result.Failure(Error.Validation("UserTenantProfile.GamerTagEmpty", "Gamer tag cannot be empty."));
        }

        GamerTag = newGamerTag.Trim();
        return Result.Success();
    }
}