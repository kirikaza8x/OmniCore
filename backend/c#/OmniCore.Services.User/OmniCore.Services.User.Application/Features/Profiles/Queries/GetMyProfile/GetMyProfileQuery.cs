namespace OmniCore.Services.User.Application.Features.Profiles.Queries.GetMyProfile;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record GetMyProfileQuery : IQuery<UserProfileResponse>;
