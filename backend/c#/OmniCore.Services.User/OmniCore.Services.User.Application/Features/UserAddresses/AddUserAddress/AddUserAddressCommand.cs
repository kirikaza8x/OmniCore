// Features/UserAddresses/Commands/AddUserAddress/AddUserAddressCommand.cs
namespace OmniCore.Services.User.Application.Features.UserAddresses.Commands.AddUserAddress;

using OmniCore.Services.User.Application.Features.UserProfiles.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record AddUserAddressCommand(
    Guid UserId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string Country,
    string ZipCode,
    bool IsPrimary) : ICommand<UserAddressResponse>;