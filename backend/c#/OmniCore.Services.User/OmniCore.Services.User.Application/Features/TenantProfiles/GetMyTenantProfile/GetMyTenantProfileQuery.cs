namespace OmniCore.Services.User.Application.Features.TenantProfiles.GetMyTenantProfile;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Application.Abstractions.Tenancy;

public record GetMyTenantProfileQuery : IQuery<TenantProfileResponse>, ITenantScopedRequest;
