// Specifications/UserProfileSpecifications.cs
namespace OmniCore.Services.User.Domain.Specifications;

using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Domain.Specifications;

public sealed class UserProfileWithSettingsSpecification : BaseSpecification<UserProfile>
{
    public UserProfileWithSettingsSpecification(UserId userId)
        : base(u => u.Id == userId && !u.IsDeleted)
    {
        AddInclude("Settings");
        ApplyNoTracking();
    }
}

public sealed class UserProfileWithAddressesSpecification : BaseSpecification<UserProfile>
{
    public UserProfileWithAddressesSpecification(UserId userId)
        : base(u => u.Id == userId && !u.IsDeleted)
    {
        AddInclude("Addresses");
        ApplyNoTracking();
    }
}

public sealed class UserProfileFullSpecification : BaseSpecification<UserProfile>
{
    public UserProfileFullSpecification(UserId userId)
        : base(u => u.Id == userId && !u.IsDeleted)
    {
        AddInclude("Settings");
        AddInclude("Addresses");
        AddInclude("TenantProfiles");
        ApplyNoTracking();
    }

    public UserProfileFullSpecification(string identifier)
        : base(u => (u.Email != null && u.Email.Value == identifier) || 
                    (u.Username != null && u.Username.Value == identifier))
    {
        AddInclude("Settings");
        AddInclude("Addresses");
        AddInclude("TenantProfiles");
        ApplyNoTracking();
    }
}