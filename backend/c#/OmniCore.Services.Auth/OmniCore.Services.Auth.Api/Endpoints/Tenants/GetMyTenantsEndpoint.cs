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

public sealed class GetMyTenantsEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapGet(Routes.MyTenants, async (ISender sender, CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new GetMyTenantsQuery(), cancellationToken);
               return result.ToOk("Tenants retrieved successfully");
           })
           .WithName("GetMyTenants")
           .RequireAuthorization()
           .Produces<ApiResult<IReadOnlyList<TenantDto>>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
