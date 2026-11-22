namespace OmniCore.Services.User.Application.Features.Addresses.RemoveMyAddress;

using OmniCore.Shared.Application.Abstractions.Messaging;

public record RemoveMyAddressCommand(Guid AddressId) : ICommand;
