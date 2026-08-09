namespace OmniCore.Services.Auth.Application.Features.Auth.Mappings;

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

    public static AccountResponse ToResponse(this AccountDto dto) => new(
        dto.Id,
        dto.Email,
        dto.Username,
        dto.IsActive,
        dto.Roles
    );

    public static AccountResponse ToResponse(this Account account) => new(
        account.Id.Value,
        account.Email?.Value,
        account.Username.Value,
        account.IsActive,
        account.AccountRoles
            .Select(ar => ar.Role?.Name ?? string.Empty)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToList()
    );
}