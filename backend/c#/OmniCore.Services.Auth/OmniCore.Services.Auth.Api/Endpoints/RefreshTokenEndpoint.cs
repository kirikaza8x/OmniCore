using OmniCore.Services.Auth.Application.Features.Auth.Commands.RefreshToken;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;

namespace OmniCore.Services.Auth.Api.Endpoints;

public sealed class RefreshTokenEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.RefreshToken, async (
               [FromBody] RefreshTokenCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Token refreshed successfully");
           })
           .WithName("RefreshToken")
           .Produces<ApiResult<AuthResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}