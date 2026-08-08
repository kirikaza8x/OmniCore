namespace OmniCore.Services.Auth.Application.Mappings;

using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Domain.Entities;

public static class AccountMappingExtensions
{
    public static AccountDto ToDto(this Account account)
    {
        return new AccountDto(
            Id: account.Id.Value,
            Email: account.Email?.Value,
            Username: account.Username.Value,
            PasswordHash: account.PasswordHash?.Value,
            IsActive: account.IsActive,
            Roles: account.AccountRoles
                .Select(ar => ar.Role?.Name ?? string.Empty)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList()
        );
    }
}