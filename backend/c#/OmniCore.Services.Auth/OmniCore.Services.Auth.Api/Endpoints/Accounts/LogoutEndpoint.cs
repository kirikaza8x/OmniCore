namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.Logout;

public sealed class LogoutEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.Logout, async (
               [FromBody] LogoutCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Logout successful");
           })
           .WithName("Logout")
           .Produces<ApiResult>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}