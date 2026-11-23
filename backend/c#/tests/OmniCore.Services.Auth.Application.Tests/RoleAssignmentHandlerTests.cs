using OmniCore.Services.Auth.Application.Features.Roles.Commands.AssignRoleToAccount;
using OmniCore.Services.Auth.Application.Features.Roles.Commands.RemoveRoleFromAccount;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;

namespace OmniCore.Services.Auth.Application.Tests;

public class RoleAssignmentHandlerTests
{
    private readonly IAccountRepository _accounts = TestSupport.Repository();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly Guid _roleId = Guid.NewGuid();

    private Account Finds()
    {
        var account = TestSupport.NewAccount();
        _accounts.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(account));
        return account;
    }

    private AssignRoleToAccountCommandHandler Assign() => new(_accounts, _roles);

    [Fact]
    public async Task Assign_UnknownRole_IsNotFound()
    {
        _roles.ExistsAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Assign().Handle(new AssignRoleToAccountCommand(Guid.NewGuid(), _roleId), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Role.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Assign_UnknownAccount_IsNotFound()
    {
        _roles.ExistsAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(true);
        _accounts.GetByIdAsync(Arg.Any<AccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Account?>(null));

        var result = await Assign().Handle(new AssignRoleToAccountCommand(Guid.NewGuid(), _roleId), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Assign_AddsTheRole_AndASecondAssignConflicts()
    {
        _roles.ExistsAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(true);
        var account = Finds();
        var command = new AssignRoleToAccountCommand(account.Id.Value, _roleId);

        var first = await Assign().Handle(command, default);
        var second = await Assign().Handle(command, default);

        Assert.True(first.IsSuccess);
        Assert.Single(account.AccountRoles);
        Assert.True(second.IsFailure);
        Assert.Equal("Account.RoleAlreadyAssigned", second.Error.Code);
    }

    [Fact]
    public async Task Remove_AssignedRole_Succeeds()
    {
        _roles.ExistsAsync(Arg.Any<RoleId>(), Arg.Any<CancellationToken>()).Returns(true);
        var account = Finds();
        await Assign().Handle(new AssignRoleToAccountCommand(account.Id.Value, _roleId), default);

        var result = await new RemoveRoleFromAccountCommandHandler(_accounts)
            .Handle(new RemoveRoleFromAccountCommand(account.Id.Value, _roleId), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(account.AccountRoles);
    }

    [Fact]
    public async Task Remove_RoleNotAssigned_IsNotFound()
    {
        var account = Finds();

        var result = await new RemoveRoleFromAccountCommandHandler(_accounts)
            .Handle(new RemoveRoleFromAccountCommand(account.Id.Value, _roleId), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Account.RoleNotAssigned", result.Error.Code);
    }
}
