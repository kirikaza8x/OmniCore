namespace OmniCore.Services.User.Domain.Entities;

using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Abstractions;
using OmniCore.Shared.Domain.DDD;

public class UserAddress : Entity<UserAddressId>
{
    public UserId UserId { get; private set; } = null!;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }

    public UserProfile UserProfile { get; private set; } = null!;

    private UserAddress() { }

    private UserAddress(UserAddressId id, UserId userId) : base(id)
    {
        UserId = userId;
    }

    /// <summary>Created and changed only through <see cref="UserProfile"/> (add, update, remove, set primary).</summary>
    internal static Result<UserAddress> Create(
        UserId userId, string addressLine1, string? addressLine2, string city, string state, string country, string zipCode)
    {
        var address = new UserAddress(UserAddressId.New(), userId);
        var result = address.Update(addressLine1, addressLine2, city, state, country, zipCode);
        return result.IsFailure ? Result.Failure<UserAddress>(result.Error) : address;
    }

    internal Result Update(
        string addressLine1, string? addressLine2, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
            return Result.Failure(Error.Validation("UserAddress.Line1Empty", "Address line 1 is required."));
        if (string.IsNullOrWhiteSpace(city))
            return Result.Failure(Error.Validation("UserAddress.CityEmpty", "City is required."));
        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure(Error.Validation("UserAddress.CountryEmpty", "Country is required."));

        if (addressLine1.Trim().Length > 150) return Result.Failure(UserProfileErrors.TooLong("AddressLine1", 150));
        if (addressLine2 is not null && addressLine2.Trim().Length > 150) return Result.Failure(UserProfileErrors.TooLong("AddressLine2", 150));
        if (city.Trim().Length > 50) return Result.Failure(UserProfileErrors.TooLong("City", 50));
        if ((state ?? string.Empty).Trim().Length > 50) return Result.Failure(UserProfileErrors.TooLong("State", 50));
        if (country.Trim().Length > 50) return Result.Failure(UserProfileErrors.TooLong("Country", 50));
        if ((zipCode ?? string.Empty).Trim().Length > 20) return Result.Failure(UserProfileErrors.TooLong("ZipCode", 20));

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2.Trim();
        City = city.Trim();
        State = (state ?? string.Empty).Trim();
        Country = country.Trim();
        ZipCode = (zipCode ?? string.Empty).Trim();
        return Result.Success();
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
