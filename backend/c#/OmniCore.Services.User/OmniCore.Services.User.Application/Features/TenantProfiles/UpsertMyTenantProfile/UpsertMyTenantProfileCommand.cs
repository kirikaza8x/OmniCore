namespace OmniCore.Services.User.Application.Features.TenantProfiles.UpsertMyTenantProfile;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Tenancy;

/// <summary>Creates the caller's profile in the active tenant on the first call, updates it afterwards.</summary>
public record UpsertMyTenantProfileCommand(string GamerTag, string? CustomAvatarUrl)
    : ICommand<TenantProfileResponse>, ITenantScopedRequest;
