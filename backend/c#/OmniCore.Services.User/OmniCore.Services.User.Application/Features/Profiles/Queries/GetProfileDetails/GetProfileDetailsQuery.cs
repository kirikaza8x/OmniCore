namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetProfileDetails;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

/// <summary>Everything we store about a user. The endpoint restricts it to admins.</summary>
public record GetProfileDetailsQuery(Guid UserId) : IQuery<UserProfileResponse>;
