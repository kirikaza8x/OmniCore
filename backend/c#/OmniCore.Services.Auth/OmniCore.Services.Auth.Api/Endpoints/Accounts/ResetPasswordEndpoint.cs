namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.ResetPassword, async (
               [FromBody] ResetPasswordCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("Password has been reset");
           })
           .WithName("ResetPassword")
           .Produces<ApiResult>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
