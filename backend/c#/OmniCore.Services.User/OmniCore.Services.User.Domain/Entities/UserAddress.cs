// Entities/UserAddress.cs
namespace OmniCore.Services.User.Domain.Entities;

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

    private UserAddress(
        UserAddressId id,
        UserId userId,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string country,
        string zipCode,
        bool isPrimary) : base(id)
    {
        UserId = userId;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
        IsPrimary = isPrimary;
    }

    public static Result<UserAddress> Create(
        UserId userId,
        string addressLine1,
        string? addressLine2,
        string city,
        string state,
        string country,
        string zipCode,
        bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(addressLine1))
        {
            return Result.Failure<UserAddress>(Error.Validation("UserAddress.Line1Empty", "Address line 1 is required."));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            return Result.Failure<UserAddress>(Error.Validation("UserAddress.CityEmpty", "City is required."));
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            return Result.Failure<UserAddress>(Error.Validation("UserAddress.CountryEmpty", "Country is required."));
        }

        return new UserAddress(
            UserAddressId.New(),
            userId,
            addressLine1.Trim(),
            addressLine2?.Trim(),
            city.Trim(),
            state.Trim(),
            country.Trim(),
            zipCode.Trim(),
            isPrimary);
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}