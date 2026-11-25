using Microsoft.Extensions.Logging.Abstractions;
using OmniCore.Services.Auth.Application.Features.Auth.Commands.AdminConfirmEmails;
using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.Repositories;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;

namespace OmniCore.Services.Auth.Application.Tests;

public class AdminConfirmEmailsHandlerTests
{
    private readonly IAccountRepository _accounts = TestSupport.Repository();
    private readonly ICurrentUserService _user = Substitute.For<ICurrentUserService>();

    private AdminConfirmEmailsCommandHandler Handler() =>
        new(_accounts, _user, NullLogger<AdminConfirmEmailsCommandHandler>.Instance);

    private void Finds(params Account[] found) =>
        _accounts.GetByIdsAsync(Arg.Any<IReadOnlyCollection<AccountId>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Account>>(found));

    [Fact]
    public async Task ConfirmsUnverified_CountsTheRest()
    {
        var fresh = TestSupport.NewAccount();
        var done = TestSupport.NewAccount();
        Assert.True(done.ConfirmEmail().IsSuccess);
        Finds(fresh, done);
        var ids = new[] { fresh.Id.Value, done.Id.Value, Guid.NewGuid() };

        var result = await Handler().Handle(new AdminConfirmEmailsCommand(ids), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(new AdminConfirmEmailsResponse(Confirmed: 1, AlreadyConfirmed: 1, Skipped: 0, NotFound: 1), result.Value);
        Assert.True(fresh.IsEmailConfirmed);
    }

    [Fact]
    public async Task DuplicateIds_AreCountedOnce()
    {
        var fresh = TestSupport.NewAccount();
        Finds(fresh);

        var result = await Handler().Handle(new AdminConfirmEmailsCommand([fresh.Id.Value, fresh.Id.Value]), default);

        Assert.Equal(1, result.Value.Confirmed);
        Assert.Equal(0, result.Value.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Validator_RejectsEmptyAndHugeBatches(int count)
    {
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

        var validation = await new AdminConfirmEmailsCommandValidator().ValidateAsync(new AdminConfirmEmailsCommand(ids));

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task Validator_AcceptsAReasonableBatch()
    {
        var validation = await new AdminConfirmEmailsCommandValidator()
            .ValidateAsync(new AdminConfirmEmailsCommand([Guid.NewGuid(), Guid.NewGuid()]));

        Assert.True(validation.IsValid);
    }
}
