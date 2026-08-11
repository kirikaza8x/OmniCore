namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Queries.GetAccountById;

public sealed class GetAccountByIdEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapGet(Routes.GetById, async (
               [FromRoute] Guid id,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new GetAccountByIdQuery(id), cancellationToken);
               return result.ToOk("Account profile retrieved successfully");
           })
           .WithName("GetAccountById")
           .RequireAuthorization()
           .Produces<ApiResult<AccountResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }
}