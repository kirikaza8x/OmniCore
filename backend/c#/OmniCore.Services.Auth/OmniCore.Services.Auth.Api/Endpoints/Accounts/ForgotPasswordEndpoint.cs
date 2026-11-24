namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestPasswordReset;

public sealed class ForgotPasswordEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.ForgotPassword, async (
               [FromBody] RequestPasswordResetCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("If the e-mail belongs to an account, a reset link has been sent");
           })
           .WithName("ForgotPassword")
           .Produces<ApiResult>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
