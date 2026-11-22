namespace OmniCore.Services.User.Application.Features.Addresses.AddMyAddress;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class AddMyAddressCommandHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : ICommandHandler<AddMyAddressCommand, UserAddressResponse>
{
    public async Task<Result<UserAddressResponse>> Handle(AddMyAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<UserAddressResponse>(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure<UserAddressResponse>(UserProfileErrors.NotFound);

        var added = profile.AddAddress(
            request.AddressLine1, request.AddressLine2, request.City, request.State ?? string.Empty,
            request.Country, request.ZipCode ?? string.Empty, request.IsPrimary);
        if (added.IsFailure) return Result.Failure<UserAddressResponse>(added.Error);

        // The profile is tracked, so the new address is inserted on save. Calling Update() here would mark it as modified (UA1).
        return added.Value.ToResponse();
    }
}
