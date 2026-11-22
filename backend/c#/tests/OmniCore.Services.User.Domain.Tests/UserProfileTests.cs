using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Events;
using OmniCore.Services.User.Domain.ValueObjects;

namespace OmniCore.Services.User.Domain.Tests;

public class UserProfileTests
{
    private static UserProfile NewProfile(string username = "alice_01", string? email = "Alice@Example.com")
    {
        var result = UserProfile.Create(Guid.NewGuid(), username, email);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        return result.Value;
    }

    private static UserAddress AddAddress(UserProfile profile, string line = "1 Main St", bool primary = false)
    {
        var result = profile.AddAddress(line, null, "Hanoi", "", "Vietnam", "100000", primary);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
        return result.Value;
    }

    // ---- Create

    [Fact]
    public void Create_uses_the_given_account_id_and_lowercases_the_email()
    {
        var accountId = Guid.NewGuid();
        var profile = UserProfile.Create(accountId, "alice_01", "Alice@Example.com").Value;

        Assert.Equal(accountId, profile.Id.Value);
        Assert.Equal("alice@example.com", profile.Email!.Value);
        Assert.True(profile.IsActive);
        Assert.Equal("alice_01", profile.DisplayName);
        Assert.Contains(profile.DomainEvents, e => e is UserProfileCreatedDomainEvent);
    }

    [Fact]
    public void Create_has_default_settings()
    {
        var settings = NewProfile().Settings;

        Assert.Equal("Dark", settings.Theme);
        Assert.Equal("en-US", settings.Language);
        Assert.True(settings.EmailNotificationsEnabled);
        Assert.True(settings.PushNotificationsEnabled);
    }

    [Fact]
    public void Create_allows_no_email()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), "alice_01", null).Value;
        Assert.Null(profile.Email);
        Assert.True(UserProfile.Create(Guid.NewGuid(), "alice_01", "  ").IsSuccess);
    }

    [Fact]
    public void Create_rejects_an_empty_id_a_bad_username_and_a_bad_email()
    {
        Assert.True(UserProfile.Create(Guid.Empty, "alice_01", null).IsFailure);
        Assert.True(UserProfile.Create(Guid.NewGuid(), "ab", null).IsFailure);
        Assert.True(UserProfile.Create(Guid.NewGuid(), "alice_01", "not-an-email").IsFailure);
    }

    [Fact]
    public void Create_rejects_a_too_long_name()
    {
        Assert.True(UserProfile.Create(Guid.NewGuid(), "alice_01", null, new string('a', 101)).IsFailure);
    }

    // ---- Personal information

    [Fact]
    public void UpdatePersonalInformation_replaces_all_fields_and_builds_the_display_name()
    {
        var profile = NewProfile();

        var result = profile.UpdatePersonalInformation(" Ann ", "Lee", "Hello", "+84 90 123 4567");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ann", profile.FirstName);
        Assert.Equal("Ann Lee", profile.DisplayName);
        Assert.Equal("Hello", profile.Bio);
        Assert.Equal("+84 90 123 4567", profile.PhoneNumber);
        Assert.Contains(profile.DomainEvents, e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void UpdatePersonalInformation_with_blank_values_clears_them_and_falls_back_to_the_username()
    {
        var profile = NewProfile();
        profile.UpdatePersonalInformation("Ann", "Lee", "Hello", "123");

        profile.UpdatePersonalInformation(" ", null, "", null);

        Assert.Null(profile.FirstName);
        Assert.Null(profile.Bio);
        Assert.Null(profile.PhoneNumber);
        Assert.Equal("alice_01", profile.DisplayName);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("<script>")]
    public void UpdatePersonalInformation_rejects_a_bad_phone_number(string phone)
    {
        Assert.True(NewProfile().UpdatePersonalInformation(null, null, null, phone).IsFailure);
    }

    [Fact]
    public void UpdatePersonalInformation_rejects_a_too_long_bio_and_keeps_the_old_values()
    {
        var profile = NewProfile();
        profile.UpdatePersonalInformation("Ann", null, "short", null);

        var result = profile.UpdatePersonalInformation("Bob", null, new string('x', 501), null);

        Assert.True(result.IsFailure);
        Assert.Equal("Ann", profile.FirstName);
    }

    // ---- Avatar

    [Fact]
    public void UpdateAvatar_sets_the_url_and_rejects_an_empty_one()
    {
        var profile = NewProfile();

        Assert.True(profile.UpdateAvatar(" ").IsFailure);
        Assert.True(profile.UpdateAvatar("https://cdn.example.com/a.png").IsSuccess);
        Assert.Equal("https://cdn.example.com/a.png", profile.AvatarUrl);
        Assert.Contains(profile.DomainEvents, e => e is UserAvatarUpdatedDomainEvent);
    }

    // ---- Settings

    [Fact]
    public void UpdateSettings_normalises_the_theme()
    {
        var profile = NewProfile();

        var result = profile.UpdateSettings("light", "vi-VN", false, true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Light", profile.Settings.Theme);
        Assert.Equal("vi-VN", profile.Settings.Language);
        Assert.False(profile.Settings.EmailNotificationsEnabled);
    }

    [Theory]
    [InlineData("Purple", "en")]
    [InlineData("Dark", "english")]
    [InlineData("Dark", "")]
    public void UpdateSettings_rejects_unknown_values_and_keeps_the_old_ones(string theme, string language)
    {
        var profile = NewProfile();

        Assert.True(profile.UpdateSettings(theme, language, true, true).IsFailure);
        Assert.Equal("Dark", profile.Settings.Theme);
        Assert.Equal("en-US", profile.Settings.Language);
    }

    // ---- Addresses

    [Fact]
    public void The_first_address_becomes_primary()
    {
        var profile = NewProfile();

        var address = AddAddress(profile);

        Assert.True(address.IsPrimary);
    }

    [Fact]
    public void A_new_primary_address_clears_the_flag_on_the_others()
    {
        var profile = NewProfile();
        var first = AddAddress(profile, "1 Main St");
        var second = AddAddress(profile, "2 Side St", primary: true);

        Assert.False(first.IsPrimary);
        Assert.True(second.IsPrimary);
        Assert.Single(profile.Addresses, a => a.IsPrimary);
    }

    [Fact]
    public void A_second_address_without_the_flag_does_not_take_over()
    {
        var profile = NewProfile();
        var first = AddAddress(profile, "1 Main St");
        var second = AddAddress(profile, "2 Side St");

        Assert.True(first.IsPrimary);
        Assert.False(second.IsPrimary);
    }

    [Fact]
    public void Removing_the_primary_address_promotes_the_next_one()
    {
        var profile = NewProfile();
        var first = AddAddress(profile, "1 Main St");
        var second = AddAddress(profile, "2 Side St");

        Assert.True(profile.RemoveAddress(first.Id).IsSuccess);

        Assert.Single(profile.Addresses);
        Assert.True(second.IsPrimary);
    }

    [Fact]
    public void Removing_an_unknown_address_fails()
    {
        Assert.True(NewProfile().RemoveAddress(UserAddressId.New()).IsFailure);
    }

    [Fact]
    public void UpdateAddress_changes_the_fields_and_can_make_it_primary()
    {
        var profile = NewProfile();
        var first = AddAddress(profile, "1 Main St");
        var second = AddAddress(profile, "2 Side St");

        var result = profile.UpdateAddress(second.Id, "3 New St", "Floor 2", "Saigon", "", "Vietnam", "700000", makePrimary: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("3 New St", second.AddressLine1);
        Assert.Equal("Floor 2", second.AddressLine2);
        Assert.True(second.IsPrimary);
        Assert.False(first.IsPrimary);
    }

    [Fact]
    public void UpdateAddress_of_an_unknown_id_or_with_missing_fields_fails()
    {
        var profile = NewProfile();
        var address = AddAddress(profile);

        Assert.True(profile.UpdateAddress(UserAddressId.New(), "x", null, "c", "", "v", "", false).IsFailure);
        Assert.True(profile.UpdateAddress(address.Id, "", null, "c", "", "v", "", false).IsFailure);
        Assert.Equal("1 Main St", address.AddressLine1);
    }

    [Fact]
    public void An_address_needs_line_1_city_and_country()
    {
        var profile = NewProfile();

        Assert.True(profile.AddAddress(" ", null, "Hanoi", "", "Vietnam", "", false).IsFailure);
        Assert.True(profile.AddAddress("1 Main St", null, "", "", "Vietnam", "", false).IsFailure);
        Assert.True(profile.AddAddress("1 Main St", null, "Hanoi", "", "", "", false).IsFailure);
        Assert.Empty(profile.Addresses);
    }

    [Fact]
    public void A_profile_holds_at_most_ten_addresses()
    {
        var profile = NewProfile();
        for (var i = 0; i < UserProfile.MaxAddresses; i++) AddAddress(profile, $"{i} Main St");

        var result = profile.AddAddress("11 Main St", null, "Hanoi", "", "Vietnam", "", false);

        Assert.True(result.IsFailure);
        Assert.Equal(UserProfile.MaxAddresses, profile.Addresses.Count);
    }

    // ---- Delete

    [Fact]
    public void SoftDelete_marks_the_profile_and_raises_the_event_once()
    {
        var profile = NewProfile();

        profile.SoftDelete();
        profile.SoftDelete();

        Assert.True(profile.IsDeleted);
        Assert.False(profile.IsActive);
        Assert.NotNull(profile.DeletedAtUtc);
        Assert.Single(profile.DomainEvents, e => e is UserProfileDeletedDomainEvent);
    }
}
