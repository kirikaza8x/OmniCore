namespace OmniCore.Services.User.Application.Features.Addresses.UpdateMyAddress;

using OmniCore.Services.User.Application;
using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class UpdateMyAddressCommandHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : ICommandHandler<UpdateMyAddressCommand, UserAddressResponse>
{
    public async Task<Result<UserAddressResponse>> Handle(UpdateMyAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure<UserAddressResponse>(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure<UserAddressResponse>(UserProfileErrors.NotFound);

        var updated = profile.UpdateAddress(
            UserAddressId.From(request.AddressId), request.AddressLine1, request.AddressLine2, request.City,
            request.State ?? string.Empty, request.Country, request.ZipCode ?? string.Empty, request.IsPrimary);

        return updated.IsFailure
            ? Result.Failure<UserAddressResponse>(updated.Error)
            : updated.Value.ToResponse();
    }
}
