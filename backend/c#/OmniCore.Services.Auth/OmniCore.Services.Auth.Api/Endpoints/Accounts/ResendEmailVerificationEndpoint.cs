namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.ResendEmailVerification;

public sealed class ResendEmailVerificationEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.ResendEmailVerification, async (
               [FromBody] ResendEmailVerificationCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("If the address belongs to an unverified account, a new link has been sent");
           })
           .WithName("ResendEmailVerification")
           .Produces<ApiResult>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
