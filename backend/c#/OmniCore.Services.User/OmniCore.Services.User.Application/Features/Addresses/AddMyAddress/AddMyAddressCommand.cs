namespace OmniCore.Services.User.Application.Features.Addresses.AddMyAddress;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record AddMyAddressCommand(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? State,
    string Country,
    string? ZipCode,
    bool IsPrimary) : ICommand<UserAddressResponse>;
