using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.User.Domain.Tests;

public class UserTenantProfileTests
{
    private static readonly TenantId Tenant = TenantId.New();
    private static readonly UserId User = UserId.From(Guid.NewGuid());
    private static readonly DateTime Now = new(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_trims_the_tag_and_keeps_the_tenant_and_user()
    {
        var result = UserTenantProfile.Create(Tenant, User, "  Neo  ", "  https://x.test/a.png ", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Neo", result.Value.GamerTag);
        Assert.Equal("https://x.test/a.png", result.Value.CustomAvatarUrl);
        Assert.Equal(Tenant, result.Value.TenantId);
        Assert.Equal(User, result.Value.UserId);
        Assert.Equal(Now, result.Value.JoinedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_an_empty_tag(string tag)
    {
        var result = UserTenantProfile.Create(Tenant, User, tag, null, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("UserTenantProfile.GamerTagEmpty", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_a_tag_over_the_limit_but_accepts_exactly_the_limit()
    {
        Assert.True(UserTenantProfile.Create(Tenant, User, new string('a', UserTenantProfile.MaxGamerTagLength), null, Now).IsSuccess);
        Assert.True(UserTenantProfile.Create(Tenant, User, new string('a', UserTenantProfile.MaxGamerTagLength + 1), null, Now).IsFailure);
    }

    [Fact]
    public void A_blank_avatar_is_stored_as_none_and_a_huge_one_is_rejected()
    {
        Assert.Null(UserTenantProfile.Create(Tenant, User, "Neo", "   ", Now).Value.CustomAvatarUrl);
        Assert.True(UserTenantProfile.Create(Tenant, User, "Neo", new string('u', UserTenantProfile.MaxAvatarUrlLength + 1), Now).IsFailure);
    }

    [Fact]
    public void Update_changes_both_fields_and_a_failed_update_changes_nothing()
    {
        var profile = UserTenantProfile.Create(Tenant, User, "Neo", "https://x.test/a.png", Now).Value;

        Assert.True(profile.Update("Trinity", null).IsSuccess);
        Assert.Equal("Trinity", profile.GamerTag);
        Assert.Null(profile.CustomAvatarUrl);

        Assert.True(profile.Update(" ", "https://x.test/b.png").IsFailure);
        Assert.Equal("Trinity", profile.GamerTag);
        Assert.Null(profile.CustomAvatarUrl);
    }
}
