namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.RequestEmailVerification;

public sealed class RequestEmailVerificationEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.RequestEmailVerification, async (
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(new RequestEmailVerificationCommand(), cancellationToken);
               return result.ToOk("Verification e-mail requested");
           })
           .WithName("RequestEmailVerification")
           .RequireAuthorization()
           .Produces<ApiResult>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
