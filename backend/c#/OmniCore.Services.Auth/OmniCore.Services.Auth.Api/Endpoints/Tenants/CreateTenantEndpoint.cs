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

public sealed class CreateTenantEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.AdminTenants, async (
               [FromBody] CreateTenantCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Tenant created successfully");
           })
           .WithName("CreateTenant")
           .RequireAuthorization()
           .RequireRoles("SuperAdmin") // platform admin only (ADR 0001: tenants are admin-created)
           .Produces<ApiResult<TenantDto>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden)
           .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
