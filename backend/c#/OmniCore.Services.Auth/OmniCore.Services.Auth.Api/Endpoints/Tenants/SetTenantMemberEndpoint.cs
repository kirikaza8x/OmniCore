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

public sealed record SetTenantMemberRequest(Guid AccountId, string Role);

public sealed class SetTenantMemberEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPut(Routes.AdminTenantMembers, async (
               Guid tenantId,
               [FromBody] SetTenantMemberRequest request,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               // The tenant comes from the route, the caller is checked by the role requirement: nothing identifying is trusted from the body.
               var result = await sender.Send(new SetTenantMemberCommand(tenantId, request.AccountId, request.Role), cancellationToken);
               return result.ToOk("Tenant member saved successfully");
           })
           .WithName("SetTenantMember")
           .RequireAuthorization()
           .RequireRoles("SuperAdmin")
           .Produces<ApiResult<TenantMemberResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden)
           .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
