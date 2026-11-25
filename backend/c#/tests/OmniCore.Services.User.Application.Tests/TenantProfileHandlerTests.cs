using OmniCore.Services.User.Application.Features.TenantProfiles.GetMyTenantProfile;
using OmniCore.Services.User.Application.Features.TenantProfiles.UpsertMyTenantProfile;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.User.Application.Tests;

public class TenantProfileHandlerTests
{
    private static readonly Guid Me = Guid.NewGuid();
    private static readonly TenantId Tenant = TenantId.New();
    private static readonly DateTime Now = new(2026, 11, 25, 9, 0, 0, DateTimeKind.Utc);

    private static ITenantContext InTenant()
    {
        var ctx = Substitute.For<ITenantContext>();
        ctx.TenantId.Returns(Tenant);
        return ctx;
    }

    private static IDateTimeProvider Clock()
    {
        var clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(Now);
        return clock;
    }

    private static IUserTenantProfileRepository TenantRepo(UserTenantProfile? existing)
    {
        var repo = Substitute.For<IUserTenantProfileRepository>();
        repo.GetForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(existing);
        return repo;
    }

    private static IUserProfileRepository ProfileExists(bool exists)
    {
        var repo = Substitute.For<IUserProfileRepository>();
        repo.ExistsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(exists);
        return repo;
    }

    [Fact]
    public async Task The_first_save_creates_a_profile_stamped_with_the_active_tenant()
    {
        var tenantRepo = TenantRepo(null);
        var handler = new UpsertMyTenantProfileCommandHandler(
            tenantRepo, ProfileExists(true), TestSupport.Caller(Me), InTenant(), Clock());

        var result = await handler.Handle(new UpsertMyTenantProfileCommand("Neo", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Neo", result.Value.GamerTag);
        Assert.Equal(Tenant.Value, result.Value.TenantId);
        tenantRepo.Received(1).Add(Arg.Is<UserTenantProfile>(p => p.TenantId == Tenant && p.UserId == UserId.From(Me)));
    }

    [Fact]
    public async Task A_later_save_updates_the_existing_profile_and_adds_nothing()
    {
        var existing = UserTenantProfile.Create(Tenant, UserId.From(Me), "Neo", null, Now).Value;
        var tenantRepo = TenantRepo(existing);
        var handler = new UpsertMyTenantProfileCommandHandler(
            tenantRepo, ProfileExists(true), TestSupport.Caller(Me), InTenant(), Clock());

        var result = await handler.Handle(new UpsertMyTenantProfileCommand("Trinity", "https://x.test/a.png"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Trinity", existing.GamerTag);
        tenantRepo.DidNotReceive().Add(Arg.Any<UserTenantProfile>());
    }

    [Fact]
    public async Task Without_a_base_profile_the_first_save_is_not_found()
    {
        var handler = new UpsertMyTenantProfileCommandHandler(
            TenantRepo(null), ProfileExists(false), TestSupport.Caller(Me), InTenant(), Clock());

        var result = await handler.Handle(new UpsertMyTenantProfileCommand("Neo", null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("UserProfile.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task An_anonymous_caller_is_unauthorized_for_both_use_cases()
    {
        var anonymous = TestSupport.Caller(null);

        var upsert = await new UpsertMyTenantProfileCommandHandler(
            TenantRepo(null), ProfileExists(true), anonymous, InTenant(), Clock())
            .Handle(new UpsertMyTenantProfileCommand("Neo", null), CancellationToken.None);
        var get = await new GetMyTenantProfileQueryHandler(TenantRepo(null), anonymous)
            .Handle(new GetMyTenantProfileQuery(), CancellationToken.None);

        Assert.Equal("UserProfile.Unauthenticated", upsert.Error.Code);
        Assert.Equal("UserProfile.Unauthenticated", get.Error.Code);
    }

    [Fact]
    public async Task Get_returns_the_profile_or_not_found()
    {
        var existing = UserTenantProfile.Create(Tenant, UserId.From(Me), "Neo", null, Now).Value;

        var found = await new GetMyTenantProfileQueryHandler(TenantRepo(existing), TestSupport.Caller(Me))
            .Handle(new GetMyTenantProfileQuery(), CancellationToken.None);
        var missing = await new GetMyTenantProfileQueryHandler(TenantRepo(null), TestSupport.Caller(Me))
            .Handle(new GetMyTenantProfileQuery(), CancellationToken.None);

        Assert.Equal("Neo", found.Value.GamerTag);
        Assert.Equal("UserTenantProfile.NotFound", missing.Error.Code);
    }

    [Fact]
    public void Both_use_cases_are_marked_as_tenant_scoped_so_the_behavior_demands_a_tenant()
    {
        Assert.IsAssignableFrom<ITenantScopedRequest>(new GetMyTenantProfileQuery());
        Assert.IsAssignableFrom<ITenantScopedRequest>(new UpsertMyTenantProfileCommand("Neo", null));
    }
}
