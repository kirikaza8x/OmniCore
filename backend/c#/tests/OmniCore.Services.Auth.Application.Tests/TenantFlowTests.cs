using OmniCore.Services.Auth.Application.Abstractions.Security;
using OmniCore.Services.Auth.Application.Features.Tenants;
using OmniCore.Services.Auth.Application.Features.Tenants.Commands;
using OmniCore.Services.Auth.Application.Features.Tenants.Queries;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Time;
using OmniCore.Shared.Domain.DDD;

namespace OmniCore.Services.Auth.Application.Tests;

public class TenantFlowTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc);

    private readonly IAccountRepository _accounts = TestSupport.Repository();
    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IJwtTokenService _jwt = TestSupport.Jwt();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public TenantFlowTests() => _clock.UtcNow.Returns(Now);

    private static TenantMembershipInfo Info(TenantId id, string slug, string role = "Member") =>
        new(id, slug, slug.ToUpperInvariant(), role, Now);

    // ---- selector ----

    [Fact]
    public void Selector_prefers_the_requested_tenant_then_the_default_then_the_oldest()
    {
        var acme = Info(TenantId.New(), "acme");
        var def = Info(Tenant.DefaultId, "default");
        var list = new[] { acme, def };

        Assert.Equal(acme, TenantSelector.Choose(list, acme.TenantId.Value));
        Assert.Equal(def, TenantSelector.Choose(list, Guid.NewGuid())); // not a member of it
        Assert.Equal(def, TenantSelector.Choose(list));
        Assert.Equal(acme, TenantSelector.Choose([acme]));
        Assert.Null(TenantSelector.Choose([]));
    }

    // ---- switch ----

    private SwitchTenantCommandHandler SwitchHandler() => new(_accounts, _tenants, _jwt, _user);

    [Fact]
    public async Task Switch_issues_a_token_for_a_tenant_the_caller_belongs_to()
    {
        var account = TestSupport.NewAccount();
        var acme = Info(TenantId.New(), "acme", TenantRoles.Admin);
        _user.UserId.Returns(account.Id.Value);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(account));
        _tenants.GetMembershipsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TenantMembershipInfo>>([acme]));

        var result = await SwitchHandler().Handle(new SwitchTenantCommand(acme.TenantId.Value), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("acme", result.Value.Tenant.Slug);
        _jwt.Received(1).GenerateToken(
            account.Id.Value, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<IEnumerable<string>>(),
            Arg.Is<TenantClaim?>(t => t != null && t.TenantId == acme.TenantId.Value && t.Role == TenantRoles.Admin));
    }

    [Fact]
    public async Task Switch_to_a_tenant_without_membership_is_forbidden_and_issues_nothing()
    {
        var account = TestSupport.NewAccount();
        _user.UserId.Returns(account.Id.Value);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(account));
        _tenants.GetMembershipsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TenantMembershipInfo>>([Info(Tenant.DefaultId, "default")]));

        var result = await SwitchHandler().Handle(new SwitchTenantCommand(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.NotAMember", result.Error.Code);
        _jwt.DidNotReceiveWithAnyArgs().GenerateToken(default, default, default, default!, default);
    }

    [Fact]
    public async Task Switch_needs_an_authenticated_caller()
    {
        _user.UserId.Returns((Guid?)null);

        var result = await SwitchHandler().Handle(new SwitchTenantCommand(Guid.NewGuid()), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.Unauthorized", result.Error.Code);
    }

    // ---- my tenants ----

    [Fact]
    public async Task My_tenants_lists_the_callers_memberships()
    {
        var account = TestSupport.NewAccount();
        _user.UserId.Returns(account.Id.Value);
        _tenants.GetMembershipsAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TenantMembershipInfo>>([Info(Tenant.DefaultId, "default", TenantRoles.Owner)]));

        var tenantContext = Substitute.For<OmniCore.Shared.Application.Abstractions.Tenancy.ITenantContext>();
        tenantContext.TenantId.Returns(Tenant.DefaultId);

        var result = await new GetMyTenantsQueryHandler(_tenants, _user, tenantContext).Handle(new GetMyTenantsQuery(), default);

        var tenant = Assert.Single(result.Value);
        Assert.Equal(("default", TenantRoles.Owner), (tenant.Slug, tenant.Role));
        Assert.True(tenant.IsCurrent);
    }

    // ---- create tenant ----

    private CreateTenantCommandHandler CreateHandler() =>
        new(_tenants, _user, _clock, Substitute.For<Microsoft.Extensions.Logging.ILogger<CreateTenantCommandHandler>>());

    [Fact]
    public async Task Create_adds_the_tenant_and_makes_the_caller_its_owner()
    {
        var admin = Guid.NewGuid();
        _user.UserId.Returns(admin);
        _tenants.SlugExistsAsync("acme", Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(new CreateTenantCommand("Acme", "Acme Inc"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(("acme", TenantRoles.Owner), (result.Value.Slug, result.Value.Role));
        _tenants.Received(1).Add(Arg.Is<Tenant>(t => t.Slug == "acme"));
        _tenants.Received(1).Add(Arg.Is<TenantMembership>(m => m.AccountId.Value == admin && m.Role == TenantRoles.Owner));
    }

    [Fact]
    public async Task Create_rejects_a_taken_slug_and_a_bad_slug()
    {
        _user.UserId.Returns(Guid.NewGuid());
        _tenants.SlugExistsAsync("acme", Arg.Any<CancellationToken>()).Returns(true);

        var taken = await CreateHandler().Handle(new CreateTenantCommand("acme", "Acme"), default);
        var bad = await CreateHandler().Handle(new CreateTenantCommand("-x", "Acme"), default);

        Assert.Equal("Tenant.SlugTaken", taken.Error.Code);
        Assert.Equal("Tenant.SlugInvalid", bad.Error.Code);
        _tenants.DidNotReceiveWithAnyArgs().Add((Tenant)null!);
    }

    // ---- set member ----

    private SetTenantMemberCommandHandler MemberHandler() =>
        new(_tenants, _accounts, _user, _clock, Substitute.For<Microsoft.Extensions.Logging.ILogger<SetTenantMemberCommandHandler>>());

    [Fact]
    public async Task SetMember_adds_a_new_membership()
    {
        var tenant = Tenant.Create("acme", "Acme", Now).Value;
        var account = TestSupport.NewAccount();
        _tenants.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(tenant));
        _tenants.GetMembershipAsync(tenant.Id, account.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantMembership?>(null));
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(account));

        var result = await MemberHandler().Handle(
            new SetTenantMemberCommand(tenant.Id.Value, account.Id.Value, TenantRoles.Member), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        _tenants.Received(1).Add(Arg.Is<TenantMembership>(m => m.TenantId == tenant.Id && m.AccountId == account.Id));
    }

    [Fact]
    public async Task SetMember_changes_the_role_of_an_existing_membership()
    {
        var tenant = Tenant.Create("acme", "Acme", Now).Value;
        var account = TestSupport.NewAccount();
        var existing = TenantMembership.Create(tenant.Id, account.Id, TenantRoles.Member, Now).Value;
        _tenants.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(tenant));
        _tenants.GetMembershipAsync(tenant.Id, account.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TenantMembership?>(existing));

        var result = await MemberHandler().Handle(
            new SetTenantMemberCommand(tenant.Id.Value, account.Id.Value, TenantRoles.Admin), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Created);
        Assert.Equal(TenantRoles.Admin, existing.Role);
        _tenants.DidNotReceiveWithAnyArgs().Add((TenantMembership)null!);
    }

    [Fact]
    public async Task SetMember_reports_unknown_tenant_and_unknown_account()
    {
        var tenant = Tenant.Create("acme", "Acme", Now).Value;
        _tenants.GetByIdAsync(Arg.Any<TenantId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(null));

        var noTenant = await MemberHandler().Handle(
            new SetTenantMemberCommand(Guid.NewGuid(), Guid.NewGuid(), TenantRoles.Member), default);
        Assert.Equal("Tenant.NotFound", noTenant.Error.Code);

        _tenants.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<Tenant?>(tenant));
        _tenants.GetMembershipAsync(Arg.Any<TenantId>(), Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TenantMembership?>(null));
        _accounts.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Account?>(null));

        var noAccount = await MemberHandler().Handle(
            new SetTenantMemberCommand(tenant.Id.Value, Guid.NewGuid(), TenantRoles.Member), default);
        Assert.Equal("Account.NotFound", noAccount.Error.Code);
    }
}
