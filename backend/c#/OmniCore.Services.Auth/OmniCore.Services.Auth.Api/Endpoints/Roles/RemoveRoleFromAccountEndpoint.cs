namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Roles.Commands.RemoveRoleFromAccount;
using OmniCore.Services.Auth.Application.Features.Roles.DTOs;

public sealed class RemoveRoleFromAccountEndpoint : ICarterModule
{
    private static readonly RoleRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.RemoveRole, async (
               [FromBody] RemoveRoleFromAccountRequest request,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var command = new RemoveRoleFromAccountCommand(request.AccountId, request.RoleId);
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Role removed from account successfully");
           })
           .WithName("RemoveRoleFromAccount")
           .RequireAuthorization()
           .Produces<ApiResult<string>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }
}