// namespace OmniCore.Services.Auth.Api.Endpoints.Roles;

// using Carter;
// using MediatR;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Routing;
// using OmniCore.Services.Auth.Api.Routing;

// public sealed class DeleteRoleEndpoint : ICarterModule
// {
//     private static readonly RoleRoutes Routes = new();

//     public void AddRoutes(IEndpointRouteBuilder app)
//     {
//         app.MapGroup(Routes.GroupPrefix)
//            .WithTags(Routes.Tag)
//            .MapDelete(Routes.Delete, async (
//                [FromRoute] Guid id,
//                ISender sender,
//                CancellationToken cancellationToken) =>
//            {
//                var result = await sender.Send(new DeleteRoleCommand(id), cancellationToken);
//                return result.ToNoContent();
//            })
//            .WithName("DeleteRole")
//            .RequireAuthorization()
//            .Produces(StatusCodes.Status204NoContent)
//            .ProducesProblem(StatusCodes.Status404NotFound);
//     }
// }