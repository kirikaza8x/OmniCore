namespace OmniCore.Services.User.Application.Abstractions;

using OmniCore.Services.User.Domain.Errors;
using OmniCore.Services.User.Domain.ValueObjects;
using OmniCore.Shared.Application.Abstractions.Authentication;
using OmniCore.Shared.Domain.Abstractions;

public static class CurrentUserExtensions
{
    /// <summary>The caller's profile id, taken from the token (ADR 0004). Fails with 401 when nobody is signed in.</summary>
    public static Result<UserId> RequireUserId(this ICurrentUserService currentUser)
    {
        var id = currentUser.UserId;
        if (id is null || id == Guid.Empty)
        {
            return Result.Failure<UserId>(UserProfileErrors.Unauthenticated);
        }

        return UserId.From(id.Value);
    }
}
