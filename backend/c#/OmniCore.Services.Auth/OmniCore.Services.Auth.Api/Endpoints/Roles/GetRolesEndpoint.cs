namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Services.Auth.Application.Features.Roles.Queries.GetRoles;

public sealed class GetRolesEndpoint : ICarterModule
{
    private static readonly RoleRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapGet(Routes.GetAll, async (
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new GetRolesQuery(), cancellationToken);
               return result.ToOk("Roles retrieved successfully");
           })
           .WithName("GetRoles")
           .RequireAuthorization()
           .Produces<ApiResult<IReadOnlyList<RoleResponse>>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}