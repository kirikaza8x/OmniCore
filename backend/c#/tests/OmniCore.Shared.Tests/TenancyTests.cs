using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using OmniCore.Shared.Application.Abstractions.Tenancy;
using OmniCore.Shared.Application.Behaviors;
using OmniCore.Shared.Contracts.Events;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;
using OmniCore.Shared.Infrastructure.Services.Authentication;

namespace OmniCore.Shared.Tests;

public class TenancyTests
{
    private static TenantContext ContextWithClaim(string? tenantClaim)
    {
        var http = new DefaultHttpContext();
        var claims = tenantClaim is null ? [] : new[] { new Claim(TenantContext.ClaimType, tenantClaim) };
        http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(http);
        return new TenantContext(accessor);
    }

    [Fact]
    public void Tenant_id_rejects_the_empty_guid_and_parses_text()
    {
        Assert.Throws<ArgumentException>(() => new TenantId(Guid.Empty));
        Assert.False(TenantId.TryParse("not-a-guid", out _));
        Assert.False(TenantId.TryParse(Guid.Empty.ToString(), out _));
        var id = TenantId.New();
        Assert.True(TenantId.TryParse(id.ToString(), out var parsed));
        Assert.Equal(id, parsed);
    }

    [Fact]
    public void Context_reads_the_tenant_claim()
    {
        var id = TenantId.New();
        Assert.Equal(id, ContextWithClaim(id.ToString()).TenantId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("garbage")]
    public void Context_without_a_valid_claim_has_no_tenant(string? claim)
    {
        Assert.Null(ContextWithClaim(claim).TenantId);
    }

    [Fact]
    public void Explicit_scope_wins_over_the_claim_and_is_restored()
    {
        var claimTenant = TenantId.New();
        var other = TenantId.New();
        var ctx = ContextWithClaim(claimTenant.ToString());

        using (ctx.BeginTenantScope(other))
        {
            Assert.Equal(other, ctx.TenantId);
        }

        Assert.Equal(claimTenant, ctx.TenantId);
    }

    [Fact]
    public void System_scope_is_off_again_after_dispose()
    {
        var ctx = ContextWithClaim(TenantId.New().ToString());

        using (ctx.BeginSystemScope())
        {
            Assert.True(ctx.IsSystem);
            Assert.Null(ctx.TenantId);
        }

        Assert.False(ctx.IsSystem);
        Assert.NotNull(ctx.TenantId);
    }

    private sealed record Ping : ITenantScopedRequest;

    [Fact]
    public async Task Behavior_answers_forbidden_without_a_tenant_and_does_not_call_the_handler()
    {
        var behavior = new TenantBehavior<Ping, Result>(ContextWithClaim(null));
        bool called = false;

        Result result = await behavior.Handle(new Ping(), () => { called = true; return Task.FromResult(Result.Success()); }, default);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.False(called);
    }

    [Fact]
    public async Task Behavior_builds_the_generic_failure_too()
    {
        var behavior = new TenantBehavior<Ping, Result<int>>(ContextWithClaim(null));

        Result<int> result = await behavior.Handle(new Ping(), () => Task.FromResult(Result.Success(1)), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Tenant.Required", result.Error.Code);
    }

    [Fact]
    public async Task Behavior_passes_through_with_a_tenant()
    {
        var behavior = new TenantBehavior<Ping, Result>(ContextWithClaim(TenantId.New().ToString()));

        Result result = await behavior.Handle(new Ping(), () => Task.FromResult(Result.Success()), default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Partition_key_keeps_tenant_and_aggregate()
    {
        var tenant = Guid.NewGuid();
        var aggregate = Guid.NewGuid();
        Assert.Equal($"{tenant:N}:{aggregate:N}", TenantPartitionKey.For(tenant, aggregate));
    }
}
