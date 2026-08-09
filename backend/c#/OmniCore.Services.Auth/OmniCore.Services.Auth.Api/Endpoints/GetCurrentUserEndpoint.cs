namespace OmniCore.Services.Auth.Api.Endpoints;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;
using OmniCore.Services.Auth.Application.Features.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapGet(Routes.Me, async (
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
               return result.ToOk("Current user profile retrieved successfully");
           })
           .WithName("GetCurrentUser")
           .RequireAuthorization()
           .Produces<ApiResult<AccountResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }
}