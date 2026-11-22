namespace OmniCore.Services.User.Application.Features.Addresses.RemoveMyAddress;

using OmniCore.Services.User.Application.Abstractions;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class RemoveMyAddressCommandHandler(IUserProfileRepository profiles, ICurrentUserService currentUser)
    : ICommandHandler<RemoveMyAddressCommand>
{
    public async Task<Result> Handle(RemoveMyAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        if (userId.IsFailure) return Result.Failure(userId.Error);

        var profile = await profiles.GetByIdAsync(userId.Value, cancellationToken);
        if (profile is null) return Result.Failure(UserProfileErrors.NotFound);

        return profile.RemoveAddress(UserAddressId.From(request.AddressId));
    }
}
