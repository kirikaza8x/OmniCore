namespace OmniCore.Services.User.Application.Features.Addresses.UpdateMyAddress;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record UpdateMyAddressCommand(
    Guid AddressId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string Country,
    string? ZipCode,
    bool IsPrimary) : ICommand<UserAddressResponse>;
