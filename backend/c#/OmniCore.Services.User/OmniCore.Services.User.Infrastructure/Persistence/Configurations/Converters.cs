namespace OmniCore.Services.User.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OmniCore.Services.User.Domain.ValueObjects;

/// <summary>One converter instance per id type, shared by a primary key and the foreign keys that point at it.</summary>
internal static class Converters
{
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(id => id.Value, value => UserId.From(value));

    public static readonly ValueConverter<UserSettingsId, Guid> UserSettingsIdConverter =
        new(id => id.Value, value => UserSettingsId.From(value));

    public static readonly ValueConverter<UserAddressId, Guid> UserAddressIdConverter =
        new(id => id.Value, value => UserAddressId.From(value));

    public static readonly ValueConverter<UserTenantProfileId, Guid> UserTenantProfileIdConverter =
        new(id => id.Value, value => UserTenantProfileId.From(value));
}
