namespace OmniCore.Services.User.Api.Endpoints;

using Microsoft.AspNetCore.Http;
using OmniCore.Services.User.Application.DTOs;
using OmniCore.Services.User.Application.Features.Addresses.AddMyAddress;
using OmniCore.Services.User.Application.Features.Addresses.GetMyAddresses;
using OmniCore.Services.User.Application.Features.Addresses.RemoveMyAddress;
using OmniCore.Services.User.Application.Features.Addresses.UpdateMyAddress;

/// <summary>The signed-in user's addresses (at most 10, one primary). The user id comes from the token.</summary>
public sealed class AddressEndpoints : ICarterModule
{
    private static readonly UserRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Routes.GroupPrefix).WithTags(Routes.Tag);

        group.MapGet(Routes.MyAddresses, async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyAddressesQuery(), ct)).ToOk("Addresses retrieved successfully"))
            .WithName("GetMyAddresses")
            .RequireAuthorization()
            .Produces<ApiResult<List<UserAddressResponse>>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost(Routes.MyAddresses, async ([FromBody] AddressRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(
                    new AddMyAddressCommand(
                        body.AddressLine1, body.AddressLine2, body.City, body.State, body.Country, body.ZipCode, body.IsPrimary), ct))
                    .ToCreated($"/{Routes.GroupPrefix}/{Routes.MyAddresses}", "Address added successfully"))
            .WithName("AddMyAddress")
            .RequireAuthorization()
            .Produces<ApiResult<UserAddressResponse>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut(Routes.MyAddress, async (
                Guid addressId, [FromBody] AddressRequest body, ISender sender, CancellationToken ct) =>
                (await sender.Send(
                    new UpdateMyAddressCommand(
                        addressId, body.AddressLine1, body.AddressLine2, body.City, body.State, body.Country, body.ZipCode, body.IsPrimary), ct))
                    .ToOk("Address updated successfully"))
            .WithName("UpdateMyAddress")
            .RequireAuthorization()
            .Produces<ApiResult<UserAddressResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete(Routes.MyAddress, async (Guid addressId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RemoveMyAddressCommand(addressId), ct)).ToNoContent())
            .WithName("RemoveMyAddress")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
