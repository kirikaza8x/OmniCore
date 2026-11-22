using OmniCore.Services.User.Application.Features.Addresses.AddMyAddress;
using OmniCore.Services.User.Application.Features.Addresses.GetMyAddresses;
using OmniCore.Services.User.Application.Features.Addresses.RemoveMyAddress;
using OmniCore.Services.User.Application.Features.Addresses.UpdateMyAddress;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMyProfile;
using OmniCore.Services.User.Application.Features.Profiles.Commands.UpdateMySettings;
using OmniCore.Services.User.Application.Features.Profiles.Queries.GetMyProfile;
using OmniCore.Services.User.Application.Features.Profiles.Queries.GetPublicProfile;
using OmniCore.Shared.Domain.Abstractions;

namespace OmniCore.Services.User.Application.Tests;

public class ProfileHandlerTests
{
    private static readonly Guid Me = Guid.NewGuid();

    // ---- every "me" use case: the caller comes from the token

    [Fact]
    public async Task Without_a_signed_in_caller_every_me_use_case_is_unauthorized()
    {
        var anonymous = TestSupport.Caller(null);
        var repo = TestSupport.Repository(TestSupport.NewProfile(Me));

        var results = new[]
        {
            (await new UpdateMyProfileCommandHandler(repo, anonymous).Handle(new UpdateMyProfileCommand("a", null, null, null), default)).IsFailure
                ? (await new UpdateMyProfileCommandHandler(repo, anonymous).Handle(new UpdateMyProfileCommand("a", null, null, null), default)).Error.Type : default,
            (await new UpdateMySettingsCommandHandler(repo, anonymous).Handle(new UpdateMySettingsCommand("Dark", "en", true, true), default)).Error.Type,
            (await new GetMyProfileQueryHandler(repo, anonymous).Handle(new GetMyProfileQuery(), default)).Error.Type,
            (await new GetMyAddressesQueryHandler(repo, anonymous).Handle(new GetMyAddressesQuery(), default)).Error.Type,
            (await new AddMyAddressCommandHandler(repo, anonymous).Handle(new AddMyAddressCommand("a", null, "c", null, "v", null, false), default)).Error.Type,
            (await new RemoveMyAddressCommandHandler(repo, anonymous).Handle(new RemoveMyAddressCommand(Guid.NewGuid()), default)).Error.Type,
        };

        Assert.All(results, type => Assert.Equal(ErrorType.Unauthorized, type));
        await repo.DidNotReceive().GetByIdAsync(Arg.Any<OmniCore.Services.User.Domain.ValueObjects.UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_signed_in_caller_without_a_profile_gets_not_found()
    {
        var caller = TestSupport.Caller(Me);
        var repo = TestSupport.Repository(null);

        var result = await new GetMyProfileQueryHandler(repo, caller).Handle(new GetMyProfileQuery(), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task The_profile_is_looked_up_by_the_token_id()
    {
        var repo = TestSupport.Repository(TestSupport.NewProfile(Me));

        await new GetMyProfileQueryHandler(repo, TestSupport.Caller(Me)).Handle(new GetMyProfileQuery(), default);

        await repo.Received(1).GetReadOnlyAsync(
            Arg.Is<OmniCore.Services.User.Domain.ValueObjects.UserId>(id => id.Value == Me), Arg.Any<CancellationToken>());
    }

    // ---- profile

    [Fact]
    public async Task UpdateMyProfile_changes_the_loaded_profile()
    {
        var profile = TestSupport.NewProfile(Me);
        var handler = new UpdateMyProfileCommandHandler(TestSupport.Repository(profile), TestSupport.Caller(Me));

        var result = await handler.Handle(new UpdateMyProfileCommand("Ann", "Lee", "Hi", null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ann Lee", result.Value.DisplayName);
        Assert.Equal("Ann", profile.FirstName);
    }

    [Fact]
    public async Task UpdateMyProfile_returns_the_domain_error_for_bad_input()
    {
        var handler = new UpdateMyProfileCommandHandler(TestSupport.Repository(TestSupport.NewProfile(Me)), TestSupport.Caller(Me));

        var result = await handler.Handle(new UpdateMyProfileCommand(null, null, null, "abc"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task UpdateMySettings_returns_the_new_settings_or_a_validation_error()
    {
        var profile = TestSupport.NewProfile(Me);
        var handler = new UpdateMySettingsCommandHandler(TestSupport.Repository(profile), TestSupport.Caller(Me));

        var ok = await handler.Handle(new UpdateMySettingsCommand("light", "vi-VN", false, false), default);
        var bad = await handler.Handle(new UpdateMySettingsCommand("Purple", "vi-VN", false, false), default);

        Assert.True(ok.IsSuccess);
        Assert.Equal("Light", ok.Value.Theme);
        Assert.True(bad.IsFailure);
        Assert.Equal("Light", profile.Settings.Theme);
    }

    [Fact]
    public async Task The_public_profile_has_no_private_data_and_hides_inactive_profiles()
    {
        var profile = TestSupport.NewProfile(Me);
        profile.UpdatePersonalInformation("Ann", "Lee", "Hi", "0123");
        var repo = TestSupport.Repository(profile);

        var result = await new GetPublicProfileQueryHandler(repo).Handle(new GetPublicProfileQuery(Me), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ann Lee", result.Value.DisplayName);
        Assert.DoesNotContain(typeof(OmniCore.Services.User.Application.DTOs.PublicProfileResponse).GetProperties(),
            p => p.Name is "Email" or "PhoneNumber" or "Addresses" or "Settings");

        profile.SoftDelete();
        var hidden = await new GetPublicProfileQueryHandler(repo).Handle(new GetPublicProfileQuery(Me), default);
        Assert.True(hidden.IsFailure);
    }

    // ---- addresses

    [Fact]
    public async Task AddMyAddress_adds_to_the_loaded_profile_and_the_first_one_is_primary()
    {
        var profile = TestSupport.NewProfile(Me);
        var handler = new AddMyAddressCommandHandler(TestSupport.Repository(profile), TestSupport.Caller(Me));

        var result = await handler.Handle(new AddMyAddressCommand("1 Main St", null, "Hanoi", null, "Vietnam", null, false), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPrimary);
        Assert.Single(profile.Addresses);
    }

    [Fact]
    public async Task UpdateMyAddress_and_RemoveMyAddress_use_the_address_of_the_callers_profile_only()
    {
        var profile = TestSupport.NewProfile(Me);
        var repo = TestSupport.Repository(profile);
        var caller = TestSupport.Caller(Me);
        var added = (await new AddMyAddressCommandHandler(repo, caller)
            .Handle(new AddMyAddressCommand("1 Main St", null, "Hanoi", null, "Vietnam", null, false), default)).Value;

        var updated = await new UpdateMyAddressCommandHandler(repo, caller)
            .Handle(new UpdateMyAddressCommand(added.Id, "2 Side St", null, "Hanoi", null, "Vietnam", null, true), default);
        var unknown = await new RemoveMyAddressCommandHandler(repo, caller).Handle(new RemoveMyAddressCommand(Guid.NewGuid()), default);
        var removed = await new RemoveMyAddressCommandHandler(repo, caller).Handle(new RemoveMyAddressCommand(added.Id), default);

        Assert.True(updated.IsSuccess);
        Assert.Equal("2 Side St", updated.Value.AddressLine1);
        Assert.Equal(ErrorType.NotFound, unknown.Error.Type);
        Assert.True(removed.IsSuccess);
        Assert.Empty(profile.Addresses);
    }
}
