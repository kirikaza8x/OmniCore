using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Domain.Tests;

public class TenantTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("acme", "acme")]
    [InlineData("  Acme-Corp  ", "acme-corp")]
    [InlineData("a", "a")]
    public void Create_normalizes_a_valid_slug(string slug, string expected)
    {
        var result = Tenant.Create(slug, "Acme", Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Slug);
        Assert.True(result.Value.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-acme")]
    [InlineData("acme-")]
    [InlineData("ac me")]
    [InlineData("acme_corp")]
    public void Create_rejects_a_bad_slug(string slug)
    {
        var result = Tenant.Create(slug, "Acme", Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.SlugInvalid", result.Error.Code);
    }

    [Fact]
    public void Create_rejects_an_empty_or_long_name()
    {
        Assert.Equal("Tenant.NameInvalid", Tenant.Create("acme", " ", Now).Error.Code);
        Assert.Equal("Tenant.NameInvalid", Tenant.Create("acme", new string('x', 101), Now).Error.Code);
    }

    [Fact]
    public void A_new_tenant_gets_its_own_id_and_can_be_deactivated()
    {
        var tenant = Tenant.Create("acme", "Acme", Now).Value;

        Assert.NotEqual(Tenant.DefaultId, tenant.Id);
        Assert.True(tenant.Deactivate().IsSuccess);
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void Membership_requires_a_known_role()
    {
        var tenant = Tenant.Create("acme", "Acme", Now).Value;

        Assert.True(TenantMembership.Create(tenant.Id, AccountId.New(), TenantRoles.Admin, Now).IsSuccess);
        var bad = TenantMembership.Create(tenant.Id, AccountId.New(), "Root", Now);
        Assert.Equal("TenantMembership.RoleInvalid", bad.Error.Code);
    }

    [Fact]
    public void Membership_role_can_change_to_a_known_role_only()
    {
        var membership = TenantMembership.Create(Tenant.DefaultId, AccountId.New(), TenantRoles.Member, Now).Value;

        Assert.True(membership.ChangeRole(TenantRoles.Owner).IsSuccess);
        Assert.Equal(TenantRoles.Owner, membership.Role);
        Assert.True(membership.ChangeRole("x").IsFailure);
        Assert.Equal(TenantRoles.Owner, membership.Role);
    }
}
