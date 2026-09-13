// Features/UserAddresses/Commands/AddUserAddress/AddUserAddressCommandHandler.cs
namespace OmniCore.Services.User.Application.Features.UserAddresses.Commands.AddUserAddress;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Services.User.Application.Features.UserProfiles.Mappings;
using OmniCore.Services.User.Domain.Entities;
using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.Repositories;
using OmniCore.Services.User.Domain.Specifications;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Messaging;
using OmniCore.Shared.Domain.Abstractions;

public sealed class AddUserAddressCommandHandler(
    IUserProfileRepository userProfileRepository) : ICommandHandler<AddUserAddressCommand, UserAddressResponse>
{
    public async Task<Result<UserAddressResponse>> Handle(
        AddUserAddressCommand request, 
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);
        var spec = new UserProfileWithAddressesSpecification(userId);
        var profile = await userProfileRepository.GetWithSpecificationAsync(spec, cancellationToken);

        if (profile is null)
        {
            return Result.Failure<UserAddressResponse>(UserProfileErrors.NotFound);
        }

        var addressResult = UserAddress.Create(
            userId,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.State,
            request.Country,
            request.ZipCode,
            request.IsPrimary);

        if (addressResult.IsFailure)
        {
            return Result.Failure<UserAddressResponse>(addressResult.Error);
        }

        profile.AddAddress(addressResult.Value);
        userProfileRepository.Update(profile);

        return Result.Success(addressResult.Value.ToResponse());
    }
}