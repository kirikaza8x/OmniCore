using OmniCore.Services.Auth.Application.Features.Auth.Commands.Register;
using OmniCore.Services.Auth.Application.Features.Auth.DTOs;

namespace OmniCore.Services.Auth.Api.Endpoints;

public sealed class RegisterEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.Register, async (
               [FromBody] RegisterCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("User registered successfully");
           })
           .WithName("Register")
           .Produces<ApiResult<AuthResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status409Conflict);
    }
}