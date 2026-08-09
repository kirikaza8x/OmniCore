namespace OmniCore.Services.Auth.Domain.Specifications;

using OmniCore.Services.Auth.Domain.Entities;
using OmniCore.Services.Auth.Domain.ValueObjects;
using OmniCore.Shared.Domain.Specifications;

public sealed class AccountWithRolesSpecification : BaseSpecification<Account>
{
    public AccountWithRolesSpecification(AccountId accountId)
        : base(a => a.Id == accountId)
    {
        AddInclude("AccountRoles.Role");
        ApplyNoTracking();
    }

    public AccountWithRolesSpecification(string identifier)
        : base(a => (a.Email != null && a.Email.Value == identifier) || 
                  (a.Username != null && a.Username.Value == identifier)) 
    {
        AddInclude("AccountRoles.Role");
        ApplyNoTracking();
    }
}