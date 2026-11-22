namespace OmniCore.Services.User.Application.Features.Addresses.GetMyAddresses;

using OmniCore.Services.User.Application.DTOs;
using OmniCore.Shared.Application.Abstractions.Messaging;

public record GetMyAddressesQuery : IQuery<List<UserAddressResponse>>;
