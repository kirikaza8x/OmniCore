namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Roles.Commands.CreateRole;
using OmniCore.Services.Auth.Application.Features.Roles.DTOs;
using OmniCore.Shared.Api.Extensions;

public sealed class CreateRoleEndpoint : ICarterModule
{
    private static readonly RoleRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.Create, async (
               [FromBody] CreateRoleCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToCreated("Role created successfully");
           })
           .WithName("CreateRole")
           .RequireAuthorization()
           .RequireRoles("Admin")
           .Produces<ApiResult<RoleResponse>>(StatusCodes.Status201Created)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status409Conflict);
    }
}