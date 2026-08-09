namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Roles.Commands.UpdateRole;
using OmniCore.Services.Auth.Application.Features.Roles.DTOs;

public sealed class UpdateRoleEndpoint : ICarterModule
{
    private static readonly RoleRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPut(Routes.Update, async (
               [FromRoute] Guid id,
               [FromBody] UpdateRoleRequest request,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var command = new UpdateRoleCommand(id, request.Name);
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Role updated successfully");
           })
           .WithName("UpdateRole")
           .RequireAuthorization()
           .Produces<ApiResult<RoleResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status409Conflict);
    }
}