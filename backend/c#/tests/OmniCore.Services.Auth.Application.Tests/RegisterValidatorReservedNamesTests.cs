using OmniCore.Services.Auth.Application.Features.Auth.Commands.Register;
using OmniCore.Services.Auth.Domain.Entities;

namespace OmniCore.Services.Auth.Application.Tests;

// Written and checked by CI only (no .NET SDK in the authoring sandbox).
public class RegisterValidatorReservedNamesTests
{
    private static readonly string[] Reserved =
    [
        "admin", "administrator", "root", "superadmin", "system", "support", "moderator", "owner",
        "null", "undefined", "api", "www", "me", "anonymous", "postmaster", "noreply", "no-reply"
    ];

    public static TheoryData<string> ReservedNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in Reserved)
        {
            data.Add(name);
            data.Add(name.ToUpperInvariant());
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ReservedNames))]
    public async Task ReservedUsername_IsRejected(string username)
    {
        var validation = await new RegisterCommandValidator()
            .ValidateAsync(new RegisterCommand(username, "alice@example.com", TestSupport.Password));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, error => error.ErrorMessage == "This username is reserved.");
    }

    [Fact]
    public async Task NormalUsername_Passes()
    {
        var validation = await new RegisterCommandValidator()
            .ValidateAsync(new RegisterCommand("alice_01", "alice@example.com", TestSupport.Password));

        Assert.True(validation.IsValid);
    }

    [Fact]
    public void FirstAdminPath_CanStillUseReservedUsername()
    {
        // The first-admin seeder creates the account through the domain, not through the registration validator.
        var result = Account.Create("admin", "first.admin@example.com", "CHANGE_ME_hash");

        Assert.True(result.IsSuccess);
        Assert.Equal("admin", result.Value.Username.Value);
    }
}
