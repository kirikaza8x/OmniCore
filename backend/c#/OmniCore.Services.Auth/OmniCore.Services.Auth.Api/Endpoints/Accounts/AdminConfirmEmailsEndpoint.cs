namespace OmniCore.Services.Auth.Api.Endpoints.Accounts;

using OmniCore.Services.Auth.Application.Features.Auth.Commands.AdminConfirmEmails;
using OmniCore.Shared.Api.Extensions;

public sealed class AdminConfirmEmailsEndpoint : ICarterModule
{
    private static readonly AuthRoutes Routes = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGroup(Routes.GroupPrefix)
           .WithTags(Routes.Tag)
           .MapPost(Routes.AdminConfirmEmails, async (
               [FromBody] AdminConfirmEmailsCommand command,
               ISender sender,
               CancellationToken cancellationToken) =>
           {
               var result = await sender.Send(command, cancellationToken);
               return result.ToOk("E-mail addresses confirmed");
           })
           .WithName("AdminConfirmEmails")
           .RequireAuthorization()
           .RequireRoles("SuperAdmin") // vouching for other people's addresses is not a routine admin action
           .Produces<ApiResult<AdminConfirmEmailsResponse>>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status400BadRequest)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
