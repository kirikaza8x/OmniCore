namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetPublicProfile;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

/// <summary>Another user's public data (display name, avatar, bio). For any signed-in caller.</summary>
public record GetPublicProfileQuery(Guid UserId) : IQuery<PublicProfileResponse>;
