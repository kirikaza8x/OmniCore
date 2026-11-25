namespace OmniCore.Services.Auth.Api.Endpoints.Tenants;

using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using OmniCore.Services.Auth.Api.Routing;
using OmniCore.Services.Auth.Application.Features.Tenants;
using OmniCore.Services.Auth.Application.Features.Tenants.Commands;
using OmniCore.Services.Auth.Application.Features.Tenants.Queries;
using OmniCore.Shared.Api.Extensions;

public sealed class SwitchTenantEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.SwitchTenant, async (
               [FromBody] SwitchTenantCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Tenant switched successfully");
           })
           .WithName("SwitchTenant")
           .RequireAuthorization()
           .Produces<ApiResult<SwitchTenantResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
