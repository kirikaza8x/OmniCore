namespace OmniCore.Services.User.Application.Features.Addresses.GetMyAddresses;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class GetMyAddressesQueryHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : IQueryHandler<GetMyAddressesQuery, List<UserAddressResponse>>
{
    public async Task<Result<List<UserAddressResponse>>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<List<UserAddressResponse>>(userId.Error);

        var profile = await profiles.GetReadOnlyAsync(userId.Value, cancellationToken);
        return profile is null
            ? Result.Failure<List<UserAddressResponse>>(UserProfileErrors.NotFound)
            : profile.Addresses.Select(a => a.ToResponse()).ToList();
    }
}
