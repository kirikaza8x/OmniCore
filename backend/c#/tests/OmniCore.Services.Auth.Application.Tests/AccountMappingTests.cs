using OmniCore.Services.Auth.Application.Features.Auth.Mappings;

namespace OmniCore.Services.Auth.Application.Tests;

public class AccountMappingTests
{
    [Fact]
    public void Response_ShowsWhetherTheEmailIsConfirmed()
    {
        var account = TestSupport.NewAccount();

        Assert.False(account.ToResponse().IsEmailConfirmed);
        Assert.False(account.ToDto().ToResponse().IsEmailConfirmed);

        Assert.True(account.ConfirmEmail().IsSuccess);

        Assert.True(account.ToResponse().IsEmailConfirmed);
        Assert.True(account.ToDto().ToResponse().IsEmailConfirmed);
    }
}
