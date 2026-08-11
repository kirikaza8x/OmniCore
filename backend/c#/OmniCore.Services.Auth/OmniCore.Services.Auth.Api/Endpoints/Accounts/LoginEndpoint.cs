using OmniCore.Services.Auth.Application.Features.Auth.Commands.Login;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;

namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;



public sealed class LoginEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.Login, async (
               [FromBody] LoginCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Login successful");
           })
           .WithName("Login")
           .Produces<ApiResult<AuthResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}