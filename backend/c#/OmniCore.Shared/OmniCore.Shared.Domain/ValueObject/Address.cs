namespace OmniCore.Shared.Domain.ValueObjects;

using OmniCore.Shared.Domain.Abstractions;

public sealed record Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string ZipCode { get; }

    private Address(string street, string city, string state, string country, string zipCode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipCode;
    }

    public static Result<Address> Create(string street, string city, string state, string country, string zipCode)
    {
        if (string.IsNullOrWhiteSpace(street) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(country) ||
            string.IsNullOrWhiteSpace(zipCode))
        {
            return Error.Validation("Address.Incomplete", "Address fields street, city, country, and zip code are mandatory.");
        }

        return new Address(street.Trim(), city.Trim(), state?.Trim() ?? string.Empty, country.Trim(), zipCode.Trim());
    }
}