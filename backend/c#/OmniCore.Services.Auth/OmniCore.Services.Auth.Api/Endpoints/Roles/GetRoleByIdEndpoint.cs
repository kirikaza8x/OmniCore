namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoleById;

public sealed class GetRoleByIdEndpoint : ICarterModule
{
    private static readonly RoleRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapGet(Routes.GetById, async (
               [FromRoute] Guid id,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new GetRoleByIdQuery(id), cancellationToken);
               return result.ToOk("Role retrieved successfully");
           })
           .WithName("GetRoleById")
           .RequireAuthorization()
           .Produces<ApiResult<RoleResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }
}